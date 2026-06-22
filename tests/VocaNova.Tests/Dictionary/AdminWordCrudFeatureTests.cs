using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Admin.Validators;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Features.Dictionary.Services;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using VocaNova.API.Infrastructure.Storage;

namespace VocaNova.Tests.Dictionary;

public class AdminWordCrudFeatureTests
{
    [Fact]
    public async Task CreateAsync_Should_Create_Word_With_Normalized_WordKey()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(new CreateWordRequest(
            " Run ",
            "a1",
            "/run-uk/",
            "/run-us/",
            "https://example.com/run.png",
            false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Word.Should().Be("Run");
        result.Value.WordKey.Should().Be("run");
        result.Value.Cefr.Should().Be(CefrLevel.A1);

        var word = await dbContext.Words.SingleAsync();
        word.Word1.Should().Be("Run");
        word.WordKey.Should().Be("run");
        word.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_409_When_WordKey_Already_Exists()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(new CreateWordRequest(" Run ", CefrLevel.A1, null, null, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("Word already exists.");
        (await dbContext.Words.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Metadata_And_Invalidate_Word_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var cache = new FakeWordDetailCache();
        var service = CreateService(dbContext, cache);

        var result = await service.UpdateAsync(
            1,
            new UpdateWordRequest(
                " sprint ",
                "b1",
                "/sprint-uk/",
                "/sprint-us/",
                "https://example.com/sprint.png",
                true));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Word.Should().Be("sprint");
        result.Value.WordKey.Should().Be("sprint");
        result.Value.Cefr.Should().Be(CefrLevel.B1);
        result.Value.IsPhrase.Should().BeTrue();
        cache.RemoveCount.Should().Be(1);

        var word = await dbContext.Words.SingleAsync(entity => entity.WordId == 1);
        word.Word1.Should().Be("sprint");
        word.WordKey.Should().Be("sprint");
        word.CefrLevel.Should().Be(CefrLevel.B1);
    }

    [Fact]
    public void CreateWordRequestValidator_Should_Reject_Invalid_Cefr()
    {
        var validator = new CreateWordRequestValidator();

        var result = validator.Validate(new CreateWordRequest("run", "Z9", null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateWordRequest.Cefr));
    }

    [Fact]
    public async Task ImportCsvAsync_Should_Import_Valid_Rows_And_Collect_Row_Errors()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var cache = new FakeWordDetailCache();
        var service = CreateService(dbContext, cache);
        var file = CreateCsvFile(
            """
            word,cefr_level,phonetic_uk,phonetic_us,word_class,english_definition,vietnamese_meaning
            run,A1,/run-uk/,/run-us/,verb,move quickly,chay
            jump,A1,,,verb,move off the ground,nhay
            swim,A2,,,verb,move through water,boi
            invalid,Z9,,,noun,invalid cefr,loi
            ,B1,,,noun,missing word,loi
            read,B1,,,verb,look at and understand text,doc
            write,B1,,,verb,mark letters on a surface,viet
            listen,A2,,,verb,give attention to sound,nghe
            speak,C1,,,verb,say words,noi
            empty,B2,,,noun,,thieu dinh nghia
            """);

        var result = await service.ImportCsvAsync(file);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ImportedWords.Should().Be(6);
        result.Value.ImportedSenses.Should().Be(7);
        result.Value.Skipped.Should().Be(3);
        result.Value.Errors.Should().HaveCount(3);
        result.Value.Errors.Select(error => error.Row).Should().Equal(5, 6, 11);
        result.Value.Errors.Select(error => error.Column).Should().Equal("cefr_level", "word", "english_definition");
        cache.RemoveCount.Should().Be(1);

        (await dbContext.Words.CountAsync()).Should().Be(7);
        (await dbContext.WordSenses.CountAsync()).Should().Be(7);

        var runSense = await dbContext.WordSenses.SingleAsync(entity => entity.WordId == 1);
        runSense.SenseOrder.Should().Be(1);
        runSense.WordClass.Should().Be("verb");

        var jump = await dbContext.Words.SingleAsync(entity => entity.WordKey == "jump");
        jump.Status.Should().Be(UserStatus.Active);
        jump.CefrLevel.Should().Be(CefrLevel.A1);
    }

    [Fact]
    public async Task SoftDeleteAsync_Should_Set_Status_Deleted_And_Invalidate_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var cache = new FakeWordDetailCache();
        var service = CreateService(dbContext, cache);

        var result = await service.SoftDeleteAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        cache.RemoveCount.Should().Be(1);

        var word = await dbContext.Words
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.WordId == 1);
        word.Status.Should().Be(UserStatus.Deleted);
        (await dbContext.Words.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RestoreAsync_Should_Restore_Deleted_Word_Using_IgnoreQueryFilters_And_Invalidate_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run", UserStatus.Deleted);
        var cache = new FakeWordDetailCache();
        var service = CreateService(dbContext, cache);

        var result = await service.RestoreAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        cache.RemoveCount.Should().Be(1);

        var word = await dbContext.Words.SingleAsync(entity => entity.WordId == 1);
        word.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task UploadAudioAsync_Should_Upload_To_Storage_And_Save_AudioAsset()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var cache = new FakeWordDetailCache();
        var audioStorage = new FakeAudioStorage("https://cdn.example.com/words/1/audio/uk/run.mp3");
        var service = CreateService(dbContext, cache, audioStorage);
        var file = CreateAudioFile("run.mp3", "audio/mpeg", 1024);

        var result = await service.UploadAudioAsync(1, new UploadWordAudioRequest
        {
            Accent = " UK ",
            File = file,
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Accent.Should().Be(AudioAccent.Uk);
        result.Value.Source.Should().Be(AudioSource.Uploaded);
        result.Value.Status.Should().Be(AudioStatus.Uploaded);
        result.Value.Url.Should().Be("https://cdn.example.com/words/1/audio/uk/run.mp3");
        audioStorage.UploadCount.Should().Be(1);
        audioStorage.LastWordId.Should().Be(1);
        audioStorage.LastAccent.Should().Be(AudioAccent.Uk);
        cache.RemoveCount.Should().Be(1);

        var audio = await dbContext.WordAudioAssets.SingleAsync();
        audio.WordId.Should().Be(1);
        audio.Accent.Should().Be(AudioAccent.Uk);
        audio.Source.Should().Be(AudioSource.Uploaded);
        audio.StorageUrl.Should().Be("https://cdn.example.com/words/1/audio/uk/run.mp3");
        audio.Status.Should().Be(AudioStatus.Uploaded);
    }

    [Fact]
    public async Task UploadAudioAsync_Should_Reject_Invalid_Mime_And_Size()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var audioStorage = new FakeAudioStorage("https://cdn.example.com/audio.mp3");
        var service = CreateService(dbContext, audioStorage: audioStorage);

        var invalidMime = await service.UploadAudioAsync(1, new UploadWordAudioRequest
        {
            Accent = "uk",
            File = CreateAudioFile("run.txt", "text/plain", 1024),
        });
        var tooLarge = await service.UploadAudioAsync(1, new UploadWordAudioRequest
        {
            Accent = "uk",
            File = CreateAudioFile("run.mp3", "audio/mpeg", (5 * 1024 * 1024) + 1),
        });

        invalidMime.IsSuccess.Should().BeFalse();
        invalidMime.Error.Should().Be("Audio MIME type must be one of: audio/mpeg, audio/wav, audio/ogg.");
        tooLarge.IsSuccess.Should().BeFalse();
        tooLarge.Error.Should().Be("Audio file must be 5MB or smaller.");
        audioStorage.UploadCount.Should().Be(0);
        (await dbContext.WordAudioAssets.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SoftDeleteAudioAsync_Should_Only_Mark_AudioAsset_Deleted()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        dbContext.WordAudioAssets.Add(new WordAudioAsset
        {
            AudioId = 10,
            WordId = 1,
            Accent = AudioAccent.Us,
            Source = AudioSource.Uploaded,
            StorageUrl = "https://cdn.example.com/run-us.mp3",
            Status = AudioStatus.Uploaded,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
        var cache = new FakeWordDetailCache();
        var service = CreateService(dbContext, cache);

        var result = await service.SoftDeleteAudioAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        cache.RemoveCount.Should().Be(1);
        (await dbContext.WordAudioAssets.CountAsync()).Should().Be(0);

        var audio = await dbContext.WordAudioAssets
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.AudioId == 10);
        audio.Status.Should().Be(AudioStatus.Deleted);
        audio.StorageUrl.Should().Be("https://cdn.example.com/run-us.mp3");
    }

    [Fact]
    public void S3AudioStorage_BuildObjectKey_Should_Be_Deterministic_And_Safe()
    {
        var timestamp = new DateTime(2026, 6, 15, 8, 9, 10, DateTimeKind.Utc);

        var key = S3AudioStorage.BuildObjectKey(42, AudioAccent.Us, "../hello world!.mp3", timestamp);

        key.Should().Be("words/42/audio/us/20260615080910-hello-world.mp3");
    }

    [Fact]
    public async Task UploadImageAsync_Should_Upload_To_Storage_And_Save_ImageUrl()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var cache = new FakeWordDetailCache();
        var imageStorage = new FakeImageStorage("https://res.cloudinary.com/demo/image/upload/v1/vocanova/words/1/run.png");
        var service = CreateService(dbContext, cache, imageStorage: imageStorage);
        var file = CreateFormFile("run.png", "image/png", 1024);

        var result = await service.UploadImageAsync(1, new UploadWordImageRequest { File = file });

        result.IsSuccess.Should().BeTrue();
        result.Value!.ImageUrl.Should().Be("https://res.cloudinary.com/demo/image/upload/v1/vocanova/words/1/run.png");
        imageStorage.UploadCount.Should().Be(1);
        imageStorage.LastWordId.Should().Be(1);
        cache.RemoveCount.Should().Be(1);

        var word = await dbContext.Words.SingleAsync(entity => entity.WordId == 1);
        word.ImageUrl.Should().Be("https://res.cloudinary.com/demo/image/upload/v1/vocanova/words/1/run.png");
    }

    [Fact]
    public async Task UploadImageAsync_Should_Reject_Invalid_Mime_And_Size()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var imageStorage = new FakeImageStorage("https://res.cloudinary.com/demo/image/upload/run.png");
        var service = CreateService(dbContext, imageStorage: imageStorage);

        var invalidMime = await service.UploadImageAsync(1, new UploadWordImageRequest
        {
            File = CreateFormFile("run.gif", "image/gif", 1024),
        });
        var tooLarge = await service.UploadImageAsync(1, new UploadWordImageRequest
        {
            File = CreateFormFile("run.png", "image/png", (5 * 1024 * 1024) + 1),
        });

        invalidMime.IsSuccess.Should().BeFalse();
        invalidMime.Error.Should().Be("Image MIME type must be one of: image/jpeg, image/png, image/webp.");
        tooLarge.IsSuccess.Should().BeFalse();
        tooLarge.Error.Should().Be("Image file must be 5MB or smaller.");
        imageStorage.UploadCount.Should().Be(0);
        (await dbContext.Words.SingleAsync(entity => entity.WordId == 1)).ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task UpdateImageUrlAsync_Should_Require_Https_And_Update_ImageUrl()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var cache = new FakeWordDetailCache();
        var service = CreateService(dbContext, cache);

        var invalid = await service.UpdateImageUrlAsync(1, new UpdateWordImageRequest("http://example.com/run.png"));
        var valid = await service.UpdateImageUrlAsync(1, new UpdateWordImageRequest("https://example.com/run.png"));

        invalid.IsSuccess.Should().BeFalse();
        invalid.Error.Should().Be("ImageUrl must be a valid HTTPS URL.");
        valid.IsSuccess.Should().BeTrue();
        valid.Value!.ImageUrl.Should().Be("https://example.com/run.png");
        cache.RemoveCount.Should().Be(1);
    }

    [Fact]
    public void CloudinaryImageStorage_BuildPublicId_Should_Be_Deterministic_And_Safe()
    {
        var timestamp = new DateTime(2026, 6, 15, 8, 9, 10, DateTimeKind.Utc);

        var publicId = CloudinaryImageStorage.BuildPublicId(42, "../hello world!.png", timestamp, "vocanova/words");

        publicId.Should().Be("vocanova/words/42/20260615080910-hello-world");
    }

    [Fact]
    public async Task CreateSenseAsync_Should_Create_Sense_And_Invalidate_Word_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        var cache = new FakeWordDetailCache();
        var service = CreateService(dbContext, cache);

        var result = await service.CreateSenseAsync(
            1,
            new CreateSenseRequest(1, "verb", "move quickly", "chay"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.SenseId.Should().BeGreaterThan(0);
        result.Value.WordClass.Should().Be("verb");
        result.Value.VietnameseMeaning.Should().Be("chay");
        cache.RemoveCount.Should().Be(1);

        var sense = await dbContext.WordSenses.SingleAsync();
        sense.WordId.Should().Be(1);
        sense.SenseOrder.Should().Be(1);
    }

    [Fact]
    public async Task UpdateSenseAsync_Should_Update_Sense_And_Invalidate_Word_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        await SeedSenseAsync(dbContext);
        var cache = new FakeWordDetailCache();
        var service = CreateService(dbContext, cache);

        var result = await service.UpdateSenseAsync(
            1,
            10,
            new UpdateSenseRequest(2, "noun", "an act of running", "su chay"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Order.Should().Be(2);
        result.Value.WordClass.Should().Be("noun");
        cache.RemoveCount.Should().Be(1);

        var sense = await dbContext.WordSenses.SingleAsync(entity => entity.SenseId == 10);
        sense.SenseOrder.Should().Be(2);
        sense.WordClass.Should().Be("noun");
    }

    [Fact]
    public async Task SoftDeleteSenseAsync_Should_Return_400_When_Schema_Does_Not_Support_Soft_Delete()
    {
        await using var dbContext = CreateDbContext();
        await SeedWordAsync(dbContext, "run", "run");
        await SeedSenseAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SoftDeleteSenseAsync(1, 10);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Sense soft delete is not supported by current database schema.");

        (await dbContext.WordSenses.CountAsync()).Should().Be(1);
    }

    [Fact]
    public void CreateSenseRequestValidator_Should_Reject_Invalid_Request()
    {
        var validator = new CreateSenseRequestValidator();

        var result = validator.Validate(new CreateSenseRequest(0, "", "", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateSenseRequest.SenseOrder));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateSenseRequest.WordClass));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateSenseRequest.EnglishDefinition));
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static WordService CreateService(
        VocaNovaDbContext dbContext,
        IWordDetailCache? wordDetailCache = null,
        IAudioStorage? audioStorage = null,
        IImageStorage? imageStorage = null)
    {
        return new WordService(
            new WordRepository(dbContext),
            wordDetailCache: wordDetailCache,
            audioStorage: audioStorage,
            imageStorage: imageStorage);
    }

    private static IFormFile CreateCsvFile(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "words.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv",
        };
    }

    private static IFormFile CreateAudioFile(string fileName, string contentType, int length)
    {
        return CreateFormFile(fileName, contentType, length);
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, int length)
    {
        var bytes = new byte[length];
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    private static async Task SeedWordAsync(
        VocaNovaDbContext dbContext,
        string word,
        string wordKey,
        string status = UserStatus.Active)
    {
        dbContext.Words.Add(new Word
        {
            WordId = 1,
            Word1 = word,
            WordKey = wordKey,
            CefrLevel = CefrLevel.A1,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedSenseAsync(VocaNovaDbContext dbContext)
    {
        dbContext.WordSenses.Add(new WordSense
        {
            SenseId = 10,
            WordId = 1,
            SenseOrder = 1,
            WordClass = "verb",
            EnglishDefinition = "move quickly",
            VietnameseMeaning = "chay",
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeWordDetailCache : IWordDetailCache
    {
        public int RemoveCount { get; private set; }

        public Task<WordDetailDto?> GetAsync(uint wordId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WordDetailDto?>(null);
        }

        public Task SetAsync(WordDetailDto word, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint wordId, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAudioStorage : IAudioStorage
    {
        private readonly string _url;

        public FakeAudioStorage(string url)
        {
            _url = url;
        }

        public int UploadCount { get; private set; }

        public uint LastWordId { get; private set; }

        public string? LastAccent { get; private set; }

        public Task<AudioStorageResult> UploadAsync(
            uint wordId,
            string accent,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            UploadCount++;
            LastWordId = wordId;
            LastAccent = accent;
            return Task.FromResult(new AudioStorageResult(
                $"words/{wordId}/audio/{accent}/{file.FileName}",
                _url));
        }
    }

    private sealed class FakeImageStorage : IImageStorage
    {
        private readonly string _url;

        public FakeImageStorage(string url)
        {
            _url = url;
        }

        public int UploadCount { get; private set; }

        public uint LastWordId { get; private set; }

        public Task<ImageStorageResult> UploadAsync(
            uint wordId,
            IFormFile file,
            string? folder = null,
            CancellationToken cancellationToken = default)
        {
            UploadCount++;
            LastWordId = wordId;
            return Task.FromResult(new ImageStorageResult($"vocanova/words/{wordId}/{file.FileName}", _url));
        }
    }
}
