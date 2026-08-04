using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Features.Lists.Repositories;
using VocaNova.API.Features.Lists.Services;
using VocaNova.API.Features.Lists.Validators;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Lists;

public class UserListFeatureTests
{
    [Fact]
    public async Task GetByUserAsync_Should_Return_Active_Lists_With_WordCount()
    {
        await using var dbContext = CreateDbContext();
        await SeedListsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetByUserAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var lists = result.Value!;
        lists.Select(list => list.ListName).Should().Equal("Travel", "Favorites");
        lists.Single(list => list.ListName == "Favorites").WordCount.Should().Be(2);
        lists.Should().NotContain(list => list.ListName == "Deleted");
    }

    [Fact]
    public async Task GetByUserAsync_Should_Return_Cached_Lists_When_Available()
    {
        await using var dbContext = CreateDbContext();
        var cachedLists = new[]
        {
            new UserListDto(99, "Cached", 3, DateTime.UtcNow),
        };
        var cache = new FakeUserListCache(cachedLists);
        var service = CreateService(dbContext, cache);

        var result = await service.GetByUserAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedLists);
        cache.GetCount.Should().Be(1);
        cache.SetCount.Should().Be(0);
        (await dbContext.UserLists.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_List_And_Invalidate_Cache()
    {
        await using var dbContext = CreateDbContext();
        var cache = new FakeUserListCache();
        var service = CreateService(dbContext, cache);

        var result = await service.CreateAsync(1, new CreateListRequest(" Favorites "));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ListName.Should().Be("Favorites");
        result.Value.WordCount.Should().Be(0);
        cache.RemoveCount.Should().Be(1);

        var list = await dbContext.UserLists.SingleAsync();
        list.UserId.Should().Be(1);
        list.ListName.Should().Be("Favorites");
        list.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_400_When_User_Already_Has_50_Active_Lists()
    {
        await using var dbContext = CreateDbContext();
        for (var index = 1; index <= AppSettings.MaxListsPerUser; index++)
        {
            dbContext.UserLists.Add(new UserList
            {
                ListId = (uint)index,
                UserId = 1,
                ListName = $"List {index}",
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMinutes(index),
            });
        }

        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(1, new CreateListRequest("Overflow"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("A user can create at most 50 lists.");
        (await dbContext.UserLists.CountAsync()).Should().Be(AppSettings.MaxListsPerUser);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_409_When_ListName_Already_Exists_CaseInsensitive()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserLists.Add(new UserList
        {
            ListId = 1,
            UserId = 1,
            ListName = "Favorites",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(1, new CreateListRequest(" favorites "));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("List name already exists.");
        (await dbContext.UserLists.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_Should_Rename_List_And_Invalidate_Cache()
    {
        await using var dbContext = CreateDbContext();
        await SeedListsAsync(dbContext);
        var cache = new FakeUserListCache();
        var service = CreateService(dbContext, cache);

        var result = await service.UpdateAsync(1, 1, new UpdateListRequest(" Favorites Updated "));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ListName.Should().Be("Favorites Updated");
        result.Value.WordCount.Should().Be(2);
        cache.RemoveCount.Should().Be(1);

        var list = await dbContext.UserLists.SingleAsync(entity => entity.ListId == 1);
        list.ListName.Should().Be("Favorites Updated");
    }

    [Fact]
    public async Task UpdateAsync_Should_Return_409_When_NewName_Already_Exists_CaseInsensitive()
    {
        await using var dbContext = CreateDbContext();
        await SeedListsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.UpdateAsync(1, 1, new UpdateListRequest(" travel "));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("List name already exists.");

        var list = await dbContext.UserLists.SingleAsync(entity => entity.ListId == 1);
        list.ListName.Should().Be("Favorites");
    }

    [Fact]
    public async Task SoftDeleteAsync_Should_Return_403_When_User_Does_Not_Own_List()
    {
        await using var dbContext = CreateDbContext();
        await SeedListsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.SoftDeleteAsync(1, 4);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Be("You do not have access to this list.");

        var list = await dbContext.UserLists.SingleAsync(entity => entity.ListId == 4);
        list.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task SoftDeleteAsync_Should_Delete_List_And_Cascade_Delete_ListWords_Without_Changing_Progress()
    {
        await using var dbContext = CreateDbContext();
        await SeedListWithWordsAndProgressAsync(dbContext, listWordCount: 10);
        var cache = new FakeUserListCache();
        var service = CreateService(dbContext, cache);

        var result = await service.SoftDeleteAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        cache.RemoveCount.Should().Be(1);

        var list = await dbContext.UserLists
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.ListId == 10);
        list.Status.Should().Be(UserStatus.Deleted);

        var listWords = await dbContext.UserListWords
            .IgnoreQueryFilters()
            .Where(entity => entity.ListId == 10)
            .ToListAsync();
        listWords.Should().HaveCount(10);
        listWords.Should().OnlyContain(listWord => listWord.Status == UserStatus.Deleted);

        var progress = await dbContext.UserWordProgresses
            .OrderBy(entity => entity.WordId)
            .ToListAsync();
        progress.Should().HaveCount(10);
        progress.Should().OnlyContain(item => item.TestCount == 5 && item.CorrectCount == 3 && item.WrongCount == 2);
    }

    [Fact]
    public async Task GetWordsAsync_Should_Return_Paginated_Words_With_ListStats()
    {
        await using var dbContext = CreateDbContext();
        await SeedListWordsForGetAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetWordsAsync(1, 20, new ListWordsQuery { Page = 1, Limit = 1 });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalItems.Should().Be(2);
        result.Value.Items.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                WordId = 2u,
                Word = "walk",
                PrimaryMeaning = "di bo",
                // Số đúng/sai lấy từ tiến độ học của từ (user_word_progress),
                // là nơi quiz submit cập nhật — không phải user_list_word_stats.
                CorrectCount = 4,
                WrongCount = 1,
                Note = "second",
            });
    }

    [Fact]
    public async Task AddWordAsync_Should_Add_Word_When_Not_In_List()
    {
        await using var dbContext = CreateDbContext();
        await SeedListAndWordAsync(dbContext, listId: 30, wordId: 1);
        var cache = new FakeUserListCache();
        var service = CreateService(dbContext, cache);

        var result = await service.AddWordAsync(
            1,
            30,
            new AddListWordRequest(1, AddMethod.Manual, " important "));

        result.IsSuccess.Should().BeTrue();
        result.Value!.WordId.Should().Be(1);
        result.Value.Note.Should().Be("important");
        cache.RemoveCount.Should().Be(1);

        var listWord = await dbContext.UserListWords.SingleAsync();
        listWord.Status.Should().Be(UserStatus.Active);
        listWord.AddMethod.Should().Be(AddMethod.Manual);
        listWord.Note.Should().Be("important");
    }

    [Fact]
    public async Task AddWordAsync_Should_Return_409_When_Word_Already_Active_In_List()
    {
        await using var dbContext = CreateDbContext();
        await SeedListAndWordAsync(dbContext, listId: 30, wordId: 1, listWordStatus: UserStatus.Active);
        var service = CreateService(dbContext);

        var result = await service.AddWordAsync(
            1,
            30,
            new AddListWordRequest(1, AddMethod.Manual, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Be("Word already exists in this list.");
        (await dbContext.UserListWords.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AddWordAsync_Should_Restore_When_Word_Is_Deleted_In_List()
    {
        await using var dbContext = CreateDbContext();
        await SeedListAndWordAsync(dbContext, listId: 30, wordId: 1, listWordStatus: UserStatus.Deleted);
        var cache = new FakeUserListCache();
        var service = CreateService(dbContext, cache);

        var result = await service.AddWordAsync(
            1,
            30,
            new AddListWordRequest(1, AddMethod.Manual, "restored"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.WordId.Should().Be(1);
        result.Value.Note.Should().Be("restored");
        cache.RemoveCount.Should().Be(1);

        var listWord = await dbContext.UserListWords
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.UserId == 1 && entity.ListId == 30 && entity.WordId == 1);
        listWord.Status.Should().Be(UserStatus.Active);
        listWord.Note.Should().Be("restored");
        (await dbContext.UserListWords.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AddWordAsync_Should_Return_404_When_Word_Does_Not_Exist()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserLists.Add(new UserList
        {
            ListId = 30,
            UserId = 1,
            ListName = "Favorites",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.AddWordAsync(
            1,
            30,
            new AddListWordRequest(99, AddMethod.Manual, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error.Should().Be("Word not found.");
        (await dbContext.UserListWords.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AddRandomWordsAsync_Should_Add_RandomTopic_Words_And_Exclude_Active_Existing()
    {
        await using var dbContext = CreateDbContext();
        await SeedRandomTopicWordsAsync(dbContext);
        var cache = new FakeUserListCache();
        var service = CreateService(dbContext, cache);

        var result = await service.AddRandomWordsAsync(
            1,
            40,
            new AddRandomListWordsRequest(7, 10, AddMethod.RandomTopic));

        result.IsSuccess.Should().BeTrue();
        result.Value!.AddedCount.Should().Be(3);
        result.Value.Words.Select(word => word.WordId).Should().BeEquivalentTo(new[] { 2u, 3u, 4u });
        cache.RemoveCount.Should().Be(3);

        var activeListWordIds = await dbContext.UserListWords
            .Where(entity => entity.UserId == 1 && entity.ListId == 40)
            .Select(entity => entity.WordId)
            .ToListAsync();
        activeListWordIds.Should().BeEquivalentTo(new[] { 1u, 2u, 3u, 4u });

        var restoredWord = await dbContext.UserListWords
            .SingleAsync(entity => entity.UserId == 1 && entity.ListId == 40 && entity.WordId == 2);
        restoredWord.Status.Should().Be(UserStatus.Active);
        restoredWord.AddMethod.Should().Be(AddMethod.RandomTopic);
    }

    [Fact]
    public async Task AddRandomWordsAsync_Should_Return_400_When_Count_Exceeds_50()
    {
        await using var dbContext = CreateDbContext();
        await SeedRandomTopicWordsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.AddRandomWordsAsync(
            1,
            40,
            new AddRandomListWordsRequest(7, 51, AddMethod.RandomTopic));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Count must be between 1 and 50.");
    }

    [Fact]
    public async Task AddRandomWordsAsync_Should_Add_Only_QuizEligible_Relation_Words()
    {
        await using var dbContext = CreateDbContext();
        await SeedRandomRelationWordsAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.AddRandomWordsAsync(
            1,
            50,
            new AddRandomListWordsRequest(null, 10, AddMethod.RandomSynonym));

        result.IsSuccess.Should().BeTrue();
        result.Value!.AddedCount.Should().Be(1);
        result.Value.Words.Should().ContainSingle()
            .Which.WordId.Should().Be(2);

        var activeListWordIds = await dbContext.UserListWords
            .Where(entity => entity.UserId == 1 && entity.ListId == 50)
            .Select(entity => entity.WordId)
            .ToListAsync();
        activeListWordIds.Should().BeEquivalentTo(new[] { 2u });
    }

    [Fact]
    public async Task RemoveWordAsync_Should_SoftDelete_ListWord_Without_Changing_Progress()
    {
        await using var dbContext = CreateDbContext();
        await SeedListWordWithProgressAsync(dbContext);
        var cache = new FakeUserListCache();
        var service = CreateService(dbContext, cache);

        var result = await service.RemoveWordAsync(1, 60, 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        cache.RemoveCount.Should().Be(1);

        var listWord = await dbContext.UserListWords
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.UserId == 1 && entity.ListId == 60 && entity.WordId == 1);
        listWord.Status.Should().Be(UserStatus.Deleted);

        var progress = await dbContext.UserWordProgresses.SingleAsync();
        progress.TestCount.Should().Be(5);
        progress.CorrectCount.Should().Be(3);
        progress.WrongCount.Should().Be(2);
    }

    [Fact]
    public async Task RemoveWordAsync_Should_Return_404_When_ListWord_Not_Active()
    {
        await using var dbContext = CreateDbContext();
        await SeedListWordWithProgressAsync(dbContext, listWordStatus: UserStatus.Deleted);
        var service = CreateService(dbContext);

        var result = await service.RemoveWordAsync(1, 60, 1);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error.Should().Be("List word not found.");
    }

    [Fact]
    public async Task UpdateWordNoteAsync_Should_Update_Note_For_Active_ListWord()
    {
        await using var dbContext = CreateDbContext();
        await SeedListWordWithProgressAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.UpdateWordNoteAsync(
            1,
            60,
            1,
            new UpdateListWordNoteRequest(" new note "));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Note.Should().Be("new note");

        var listWord = await dbContext.UserListWords.SingleAsync();
        listWord.Note.Should().Be("new note");
    }

    [Fact]
    public async Task UpdateWordNoteAsync_Should_Return_404_When_ListWord_Not_Active()
    {
        await using var dbContext = CreateDbContext();
        await SeedListWordWithProgressAsync(dbContext, listWordStatus: UserStatus.Deleted);
        var service = CreateService(dbContext);

        var result = await service.UpdateWordNoteAsync(
            1,
            60,
            1,
            new UpdateListWordNoteRequest("new note"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error.Should().Be("List word not found.");
    }

    [Fact]
    public void CreateListRequestValidator_Should_Reject_Empty_Name()
    {
        var validator = new CreateListRequestValidator();

        var result = validator.Validate(new CreateListRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateListRequest.ListName));
    }

    [Fact]
    public void UpdateListRequestValidator_Should_Reject_Empty_Name()
    {
        var validator = new UpdateListRequestValidator();

        var result = validator.Validate(new UpdateListRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateListRequest.ListName));
    }

    [Fact]
    public void AddListWordRequestValidator_Should_Reject_Invalid_Request()
    {
        var validator = new AddListWordRequestValidator();
        var longNote = new string('a', 1001);

        var result = validator.Validate(new AddListWordRequest(0, "invalid", longNote));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(AddListWordRequest.WordId));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(AddListWordRequest.AddMethod));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(AddListWordRequest.Note));
    }

    [Fact]
    public void AddRandomListWordsRequestValidator_Should_Reject_Invalid_Request()
    {
        var validator = new AddRandomListWordsRequestValidator();

        var result = validator.Validate(new AddRandomListWordsRequest(null, 51, AddMethod.Manual));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(AddRandomListWordsRequest.Count));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(AddRandomListWordsRequest.Method));
    }

    [Fact]
    public void UpdateListWordNoteRequestValidator_Should_Reject_Too_Long_Note()
    {
        var validator = new UpdateListWordNoteRequestValidator();

        var result = validator.Validate(new UpdateListWordNoteRequest(new string('a', 1001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateListWordNoteRequest.Note));
    }

    private static VocaNovaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VocaNovaDbContext(options);
    }

    private static UserListService CreateService(
        VocaNovaDbContext dbContext,
        IUserListCache? userListCache = null)
    {
        return new UserListService(
            new UserListRepository(dbContext),
            userListCache);
    }

    private static async Task SeedListsAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.Words.AddRange(
            new Word { WordId = 1, Word1 = "one", WordKey = "one", Status = UserStatus.Active, CreatedAt = now, UpdatedAt = now },
            new Word { WordId = 2, Word1 = "two", WordKey = "two", Status = UserStatus.Active, CreatedAt = now, UpdatedAt = now },
            new Word { WordId = 3, Word1 = "three", WordKey = "three", Status = UserStatus.Active, CreatedAt = now, UpdatedAt = now },
            new Word { WordId = 4, Word1 = "four", WordKey = "four", Status = UserStatus.Deleted, CreatedAt = now, UpdatedAt = now });
        dbContext.UserLists.AddRange(
            new UserList
            {
                ListId = 1,
                UserId = 1,
                ListName = "Favorites",
                Status = UserStatus.Active,
                CreatedAt = now.AddMinutes(-10),
            },
            new UserList
            {
                ListId = 2,
                UserId = 1,
                ListName = "Travel",
                Status = UserStatus.Active,
                CreatedAt = now,
            },
            new UserList
            {
                ListId = 3,
                UserId = 1,
                ListName = "Deleted",
                Status = UserStatus.Deleted,
                CreatedAt = now.AddMinutes(-5),
            },
            new UserList
            {
                ListId = 4,
                UserId = 2,
                ListName = "Other User",
                Status = UserStatus.Active,
                CreatedAt = now,
            });

        dbContext.UserListWords.AddRange(
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 1,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = now,
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 2,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = now,
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 3,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Deleted,
                AddedAt = now,
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 1,
                WordId = 4,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = now,
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedListWithWordsAndProgressAsync(
        VocaNovaDbContext dbContext,
        int listWordCount)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = 10,
            UserId = 1,
            ListName = "Cascade",
            Status = UserStatus.Active,
            CreatedAt = now,
        });

        for (var index = 1; index <= listWordCount; index++)
        {
            dbContext.UserListWords.Add(new UserListWord
            {
                UserId = 1,
                ListId = 10,
                WordId = (uint)index,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = now.AddMinutes(index),
            });

            dbContext.UserWordProgresses.Add(new UserWordProgress
            {
                ProgressId = (uint)index,
                UserId = 1,
                WordId = (uint)index,
                TestCount = 5,
                CorrectCount = 3,
                WrongCount = 2,
                ConsecutiveCorrect = 1,
                IsInWrongList = false,
                MasteryLevel = 2,
                SrsInterval = 1,
                EaseFactor = 2.5f,
                UpdatedAt = now,
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedListWordsForGetAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = 20,
            UserId = 1,
            ListName = "Stats",
            Status = UserStatus.Active,
            CreatedAt = now,
        });

        dbContext.Words.AddRange(
            new Word
            {
                WordId = 1,
                Word1 = "run",
                WordKey = "run",
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
                WordSenses =
                {
                    new WordSense
                    {
                        SenseId = 1,
                        WordId = 1,
                        SenseOrder = 1,
                        WordClass = "verb",
                        EnglishDefinition = "move quickly",
                        VietnameseMeaning = "chay",
                    },
                },
            },
            new Word
            {
                WordId = 2,
                Word1 = "walk",
                WordKey = "walk",
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
                WordSenses =
                {
                    new WordSense
                    {
                        SenseId = 2,
                        WordId = 2,
                        SenseOrder = 1,
                        WordClass = "verb",
                        EnglishDefinition = "move on foot",
                        VietnameseMeaning = "di bo",
                    },
                },
            });

        dbContext.UserListWords.AddRange(
            new UserListWord
            {
                UserId = 1,
                ListId = 20,
                WordId = 1,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                Note = "first",
                AddedAt = now.AddMinutes(-5),
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 20,
                WordId = 2,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                Note = "second",
                AddedAt = now,
            });

        // Tiến độ học của từ được kiểm tra (walk = word 2); màn danh sách phải
        // hiển thị đúng các con số này thay vì luôn 0.
        dbContext.UserWordProgresses.Add(new UserWordProgress
        {
            UserId = 1,
            WordId = 2,
            TestCount = 5,
            CorrectCount = 4,
            WrongCount = 1,
            ConsecutiveCorrect = 1,
            IsInWrongList = false,
            MasteryLevel = 0,
            SrsInterval = 1,
            EaseFactor = 2.5f,
            UpdatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedListAndWordAsync(
        VocaNovaDbContext dbContext,
        uint listId,
        uint wordId,
        string? listWordStatus = null)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = listId,
            UserId = 1,
            ListName = "Favorites",
            Status = UserStatus.Active,
            CreatedAt = now,
        });

        dbContext.Words.Add(new Word
        {
            WordId = wordId,
            Word1 = "run",
            WordKey = "run",
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            WordSenses =
            {
                new WordSense
                {
                    SenseId = wordId,
                    WordId = wordId,
                    SenseOrder = 1,
                    WordClass = "verb",
                    EnglishDefinition = "move quickly",
                    VietnameseMeaning = "chay",
                },
            },
        });

        if (listWordStatus is not null)
        {
            dbContext.UserListWords.Add(new UserListWord
            {
                UserId = 1,
                ListId = listId,
                WordId = wordId,
                AddMethod = AddMethod.Manual,
                Status = listWordStatus,
                AddedAt = now,
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRandomTopicWordsAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = 40,
            UserId = 1,
            ListName = "Random Topic",
            Status = UserStatus.Active,
            CreatedAt = now,
        });

        dbContext.Topics.Add(new Topic
        {
            TopicId = 7,
            TopicName = "Travel",
            Status = UserStatus.Active,
        });

        for (var index = 1; index <= 5; index++)
        {
            dbContext.Words.Add(new Word
            {
                WordId = (uint)index,
                Word1 = $"word-{index}",
                WordKey = $"word-{index}",
                Status = index == 5 ? UserStatus.Deleted : UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
            });

            dbContext.WordTopics.Add(new WordTopic
            {
                WordId = (uint)index,
                TopicId = 7,
            });
        }

        dbContext.UserListWords.AddRange(
            new UserListWord
            {
                UserId = 1,
                ListId = 40,
                WordId = 1,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Active,
                AddedAt = now,
            },
            new UserListWord
            {
                UserId = 1,
                ListId = 40,
                WordId = 2,
                AddMethod = AddMethod.Manual,
                Status = UserStatus.Deleted,
                AddedAt = now,
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRandomRelationWordsAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = 50,
            UserId = 1,
            ListName = "Random Relation",
            Status = UserStatus.Active,
            CreatedAt = now,
        });

        for (var index = 1; index <= 4; index++)
        {
            dbContext.Words.Add(new Word
            {
                WordId = (uint)index,
                Word1 = $"relation-word-{index}",
                WordKey = $"relation-word-{index}",
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        dbContext.WordRelations.AddRange(
            new WordRelation
            {
                RelationId = 1,
                WordId = 1,
                RelationType = "synonym",
                RelatedWord = "relation-word-2",
                RelatedWordId = 2,
                IsQuizEligible = true,
            },
            new WordRelation
            {
                RelationId = 2,
                WordId = 1,
                RelationType = "synonym",
                RelatedWord = "relation-word-3",
                RelatedWordId = 3,
                IsQuizEligible = false,
            },
            new WordRelation
            {
                RelationId = 3,
                WordId = 1,
                RelationType = "antonym",
                RelatedWord = "relation-word-4",
                RelatedWordId = 4,
                IsQuizEligible = true,
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedListWordWithProgressAsync(
        VocaNovaDbContext dbContext,
        string listWordStatus = UserStatus.Active)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = 60,
            UserId = 1,
            ListName = "Remove Note",
            Status = UserStatus.Active,
            CreatedAt = now,
        });

        dbContext.Words.Add(new Word
        {
            WordId = 1,
            Word1 = "run",
            WordKey = "run",
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        });

        dbContext.UserListWords.Add(new UserListWord
        {
            UserId = 1,
            ListId = 60,
            WordId = 1,
            AddMethod = AddMethod.Manual,
            Note = "old note",
            Status = listWordStatus,
            AddedAt = now,
        });

        dbContext.UserWordProgresses.Add(new UserWordProgress
        {
            ProgressId = 1,
            UserId = 1,
            WordId = 1,
            TestCount = 5,
            CorrectCount = 3,
            WrongCount = 2,
            ConsecutiveCorrect = 1,
            IsInWrongList = false,
            MasteryLevel = 2,
            SrsInterval = 1,
            EaseFactor = 2.5f,
            UpdatedAt = now,
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeUserListCache : IUserListCache
    {
        private readonly IReadOnlyCollection<UserListDto>? _cachedLists;

        public FakeUserListCache(IReadOnlyCollection<UserListDto>? cachedLists = null)
        {
            _cachedLists = cachedLists;
        }

        public int GetCount { get; private set; }

        public int SetCount { get; private set; }

        public int RemoveCount { get; private set; }

        public Task<IReadOnlyCollection<UserListDto>?> GetAsync(
            uint userId,
            CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(_cachedLists);
        }

        public Task SetAsync(
            uint userId,
            IReadOnlyCollection<UserListDto> lists,
            CancellationToken cancellationToken = default)
        {
            SetCount++;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            return Task.CompletedTask;
        }
    }
}
