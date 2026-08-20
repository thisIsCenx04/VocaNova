using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.DAL.Repositories;

public sealed class QuizQuestionRepository : IQuizQuestionRepository
{
    // Một câu hỏi chỉ cần 3 đáp án nhiễu; lấy dư để còn chỗ loại trùng nhau và
    // loại những ứng viên trùng với đáp án đúng.
    private const int MaxCandidates = 48;
    private const int MinCandidates = 12;
    private const int SampleSize = 200;

    private readonly VocaNovaDbContext _dbContext;

    public QuizQuestionRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QuizQuestionWord?> FindQuestionWordAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words
            .AsNoTracking()
            .Include(word => word.WordSenses)
            .Include(word => word.WordTopics)
            .Where(word => word.WordId == wordId && word.Status == UserStatus.Active)
            .SingleOrDefaultAsync(cancellationToken);

        return word is null ? null : MapToQuestionWord(word);
    }

    public async Task<IReadOnlyCollection<QuizQuestionWord>> GetDistractorsAsync(
        uint excludingWordId,
        string wordClass,
        IReadOnlyCollection<uint> topicIds,
        CancellationToken cancellationToken = default)
    {
        var maxWordId = await _dbContext.Words
            .AsNoTracking()
            .MaxAsync(word => (uint?)word.WordId, cancellationToken) ?? 0;

        if (maxWordId == 0)
        {
            return Array.Empty<QuizQuestionWord>();
        }

        // Quét cả bảng words chỉ để lấy 3 đáp án nhiễu là quá đắt khi kho từ
        // lớn. Bốc trước một nhúm word_id ngẫu nhiên để truy vấn bám index rồi
        // mới lọc theo word_class, thay vì lọc trên toàn bộ bảng.
        if (maxWordId > SampleSize)
        {
            var sampled = await FetchCandidatesAsync(
                SampleWordIds(maxWordId, excludingWordId),
                excludingWordId,
                wordClass,
                topicIds,
                cancellationToken);

            if (sampled.Count >= MinCandidates)
            {
                return sampled;
            }
        }

        // Word class hiếm (determiner, exclamation...) hầu như không lọt vào
        // mẫu ngẫu nhiên, và kho từ nhỏ thì lấy mẫu cũng không còn ý nghĩa —
        // cả hai trường hợp đều rơi về đây.
        return await FetchCandidatesAsync(
            sampleWordIds: null,
            excludingWordId,
            wordClass,
            topicIds,
            cancellationToken);
    }

    private async Task<List<QuizQuestionWord>> FetchCandidatesAsync(
        IReadOnlyCollection<uint>? sampleWordIds,
        uint excludingWordId,
        string wordClass,
        IReadOnlyCollection<uint> topicIds,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Words
            .AsNoTracking()
            .Where(word => word.WordId != excludingWordId && word.Status == UserStatus.Active);

        if (sampleWordIds is not null)
        {
            query = query.Where(word => sampleWordIds.Contains(word.WordId));
        }

        query = topicIds.Count > 0
            ? query.Where(word => word.WordSenses.Any(sense => sense.WordClass == wordClass)
                || word.WordTopics.Any(wordTopic => topicIds.Contains(wordTopic.TopicId)))
            : query.Where(word => word.WordSenses.Any(sense => sense.WordClass == wordClass));

        // Nhánh lấy mẫu đã bị chặn sẵn ở SampleSize nên không cần cắt thêm:
        // cắt trong SQL sẽ giữ lại đúng những word_id nhỏ nhất và khiến đáp án
        // nhiễu không bao giờ rơi vào nửa sau của kho từ. Nhánh fallback quét
        // không giới hạn nên vẫn phải cắt.
        var idQuery = query.Select(word => word.WordId);
        if (sampleWordIds is null)
        {
            idQuery = idQuery.Take(MaxCandidates);
        }

        var wordIds = await idQuery.ToListAsync(cancellationToken);

        if (wordIds.Count == 0)
        {
            return new List<QuizQuestionWord>();
        }

        // Chỉ đọc đúng những cột dùng để dựng đáp án nhiễu. Include() trước đây
        // kéo về cả entity graph và nhân bản dòng theo số nghĩa của mỗi từ.
        var senses = await _dbContext.WordSenses
            .AsNoTracking()
            .Where(sense => wordIds.Contains(sense.WordId))
            .Select(sense => new CandidateSense(
                sense.WordId,
                sense.Word.Word1,
                sense.SenseId,
                sense.WordClass,
                sense.EnglishDefinition,
                sense.VietnameseMeaning,
                sense.SenseOrder))
            .ToListAsync(cancellationToken);

        return senses
            .GroupBy(sense => sense.WordId)
            .Select(group => ToQuestionWord(group, wordClass))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderBy(_ => Random.Shared.Next())
            .Take(MaxCandidates)
            .ToList();
    }

    private static HashSet<uint> SampleWordIds(uint maxWordId, uint excludingWordId)
    {
        // Chỉ gọi khi maxWordId > SampleSize, nên luôn còn đủ giá trị phân biệt
        // để vòng lặp kết thúc.
        var wordIds = new HashSet<uint>(SampleSize);
        while (wordIds.Count < SampleSize)
        {
            wordIds.Add((uint)Random.Shared.NextInt64(1, (long)maxWordId + 1));
        }

        wordIds.Remove(excludingWordId);
        return wordIds;
    }

    /// <summary>
    /// TopicIds để rỗng: phía gọi chỉ đọc Word/VietnameseMeaning của ứng viên
    /// để dựng đáp án nhiễu, nên không cần tải quan hệ chủ đề.
    /// </summary>
    private static QuizQuestionWord? ToQuestionWord(
        IEnumerable<CandidateSense> senses,
        string preferredWordClass)
    {
        var ordered = senses
            .OrderBy(sense => sense.SenseOrder)
            .ThenBy(sense => sense.SenseId)
            .ToList();

        var sense = ordered.FirstOrDefault(item => item.WordClass == preferredWordClass)
            ?? ordered.FirstOrDefault();

        return sense is null
            ? null
            : new QuizQuestionWord(
                sense.WordId,
                sense.Word,
                sense.SenseId,
                sense.WordClass,
                sense.EnglishDefinition,
                sense.VietnameseMeaning,
                Array.Empty<uint>());
    }

    private static QuizQuestionWord? MapToQuestionWord(Word word, string? preferredWordClass = null)
    {
        var sense = word.WordSenses
            .Where(sense => preferredWordClass is null || sense.WordClass == preferredWordClass)
            .OrderBy(sense => sense.SenseOrder)
            .ThenBy(sense => sense.SenseId)
            .FirstOrDefault()
            ?? word.WordSenses
                .OrderBy(sense => sense.SenseOrder)
                .ThenBy(sense => sense.SenseId)
                .FirstOrDefault();

        if (sense is null)
        {
            return null;
        }

        return new QuizQuestionWord(
            word.WordId,
            word.Word1,
            sense.SenseId,
            sense.WordClass,
            sense.EnglishDefinition,
            sense.VietnameseMeaning,
            word.WordTopics
                .Select(wordTopic => wordTopic.TopicId)
                .ToArray());
    }

    private sealed record CandidateSense(
        uint WordId,
        string Word,
        uint SenseId,
        string WordClass,
        string EnglishDefinition,
        string? VietnameseMeaning,
        int SenseOrder);
}
