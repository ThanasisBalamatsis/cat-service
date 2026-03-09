using Domain.Jobs;
using FluentAssertions;

namespace Domain.Tests.Unit.Jobs;

public class CatFetchJobTests
{
    [Fact]
    public void CatFetchJob_ShouldAllowStatusChange()
    {
        // Arrange
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        // Act
        job.Status = CatFetchJobStatus.Running;

        // Assert
        job.Status.Should().Be(CatFetchJobStatus.Running);
    }

    [Fact]
    public void CatFetchJobStatus_ShouldHaveExpectedValues()
    {
        // Assert
        ((int)CatFetchJobStatus.Pending).Should().Be(0);
        ((int)CatFetchJobStatus.Running).Should().Be(1);
        ((int)CatFetchJobStatus.Completed).Should().Be(2);
        ((int)CatFetchJobStatus.Failed).Should().Be(3);
    }

    [Fact]
    public void CatFetchJob_ErrorMessage_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        // Assert
        job.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void CatFetchJob_CompletedAt_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        // Assert
        job.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void CatFetchJob_ShouldAllowSettingErrorMessage()
    {
        // Arrange
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Failed,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        // Act
        job.ErrorMessage = "API call failed";

        // Assert
        job.ErrorMessage.Should().Be("API call failed");
    }

    [Fact]
    public void CatFetchJob_ShouldAllowSettingCompletedAt()
    {
        // Arrange
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 25
        };
        var completedAt = DateTime.UtcNow;

        // Act
        job.CompletedAt = completedAt;

        // Assert
        job.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void CatFetchJob_ShouldTrackCatsFetched()
    {
        // Arrange
        var job = new CatFetchJob
        {
            Status = CatFetchJobStatus.Running,
            CreatedAt = DateTime.UtcNow,
            CatsFetched = 0
        };

        // Act
        job.CatsFetched = 15;

        // Assert
        job.CatsFetched.Should().Be(15);
    }
}
