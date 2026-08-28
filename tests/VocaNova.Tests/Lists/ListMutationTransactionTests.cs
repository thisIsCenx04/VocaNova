using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.Tests.Lists;

public sealed class ListMutationTransactionTests
{
    [Fact]
    public async Task Random_Add_Should_Keep_First_Word_When_A_Later_Save_Fails()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = CreateOptions(databaseName);
        var cache = new RecordingCache();
        await using (var dbContext = new FailingSaveDbContext(options))
        {
            await SeedRandomWordsAsync(dbContext);
            dbContext.FailOnSaveCall = dbContext.SaveCallCount + 2;
            var service = new ListMutationService(new ListMutationRepository(dbContext), cache);

            var action = async () => await service.AddRandomWordsAsync(
                1,
                10,
                new AddRandomListWordsCommand(null, 2, AddMethod.RandomTopic));

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Injected save failure.");
        }

        await using var verification = new VocaNovaDbContext(options);
        (await verification.UserListWords.CountAsync()).Should().Be(1);
        cache.RemoveCount.Should().Be(1);
    }

    [Fact]
    public async Task Personal_Topic_Add_Should_Keep_Reserved_List_When_Membership_Save_Fails()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = CreateOptions(databaseName);
        var cache = new RecordingCache();
        await using (var dbContext = new FailingSaveDbContext(options))
        {
            await SeedPersonalTopicAsync(dbContext);
            dbContext.FailOnSaveCall = dbContext.SaveCallCount + 2;
            var listRepository = new ListMutationRepository(dbContext);
            var service = new PersonalTopicMutationService(
                new PersonalTopicMutationRepository(dbContext),
                listRepository,
                cache);

            var action = async () => await service.AddWordAsync(
                1,
                7,
                new AddPersonalTopicWordCommand(1, null));

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Injected save failure.");
        }

        await using var verification = new VocaNovaDbContext(options);
        var reservedList = await verification.UserLists.SingleAsync();
        reservedList.ListName.Should().Be(PersonalTopicListName.For(7));
        reservedList.Status.Should().Be(UserStatus.Active);
        (await verification.UserListWords.CountAsync()).Should().Be(0);
        cache.RemoveCount.Should().Be(0);
    }

    private static DbContextOptions<VocaNovaDbContext> CreateOptions(string databaseName) =>
        new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

    private static async Task SeedRandomWordsAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.UserLists.Add(new UserList
        {
            ListId = 10,
            UserId = 1,
            ListName = "Travel",
            Status = UserStatus.Active,
            CreatedAt = now,
        });
        dbContext.Words.AddRange(
            NewWord(1, "one", now),
            NewWord(2, "two", now));
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedPersonalTopicAsync(VocaNovaDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        dbContext.Topics.Add(new Topic
        {
            TopicId = 7,
            TopicName = "Travel",
            Status = UserStatus.Active,
        });
        dbContext.Words.Add(NewWord(1, "walk", now));
        dbContext.WordTopics.Add(new EntityWordTopic { TopicId = 7, WordId = 1 });
        await dbContext.SaveChangesAsync();
    }

    private static Word NewWord(uint wordId, string value, DateTime now) =>
        new()
        {
            WordId = wordId,
            Word1 = value,
            WordKey = value,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private sealed class FailingSaveDbContext : VocaNovaDbContext
    {
        public FailingSaveDbContext(DbContextOptions<VocaNovaDbContext> options)
            : base(options)
        {
        }

        public int SaveCallCount { get; private set; }

        public int FailOnSaveCall { get; set; } = int.MaxValue;

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            if (SaveCallCount == FailOnSaveCall)
            {
                throw new InvalidOperationException("Injected save failure.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class RecordingCache : IUserListCache
    {
        public int RemoveCount { get; private set; }

        public Task<IReadOnlyCollection<UserListSummary>?> GetAsync(
            uint userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<UserListSummary>?>(null);

        public Task SetAsync(
            uint userId,
            IReadOnlyCollection<UserListSummary> lists,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveAsync(uint userId, CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            return Task.CompletedTask;
        }
    }
}
