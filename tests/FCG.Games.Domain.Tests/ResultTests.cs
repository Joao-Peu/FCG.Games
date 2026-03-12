using FCG.Games.Domain.ValueObjects;
using Xunit;

namespace FCG.Games.Domain.Tests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldHaveIsSuccessTrue()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ShouldHaveIsFailureTrue()
    {
        var error = new Error("Test.Error", "Something went wrong");
        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Success_Generic_ShouldContainValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_Generic_ShouldContainError()
    {
        var error = new Error("Test.Error", "Something went wrong");
        var result = Result.Failure<int>(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Error_None_ShouldHaveEmptyCodeAndMessage()
    {
        Assert.Equal(string.Empty, Error.None.Code);
        Assert.Equal(string.Empty, Error.None.Message);
    }

    [Fact]
    public void Predefined_Errors_ShouldHaveCorrectCodes()
    {
        Assert.Equal("Game.NotFound", Errors.GameNotFound.Code);
        Assert.Equal("Game.AlreadyOwned", Errors.AlreadyOwned.Code);
        Assert.Equal("Order.Pending", Errors.PendingOrder.Code);
    }
}
