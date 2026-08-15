using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Exceptions;
using Notifliwy.Related;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Related;

/// <summary>
/// Unit tests for <see cref="MultiplyServiceInstance{TInstance}"/>
/// </summary>
public class MultiplyServiceInstanceTests
{
    private class TestService
    {
        public int Value { get; init; }
    }

    [Fact]
    public void Constructor_WithSingleService_ShouldSetSingleProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedService = new TestService { Value = 42 };
        services.AddSingleton(expectedService);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var instance = new MultiplyServiceInstance<TestService>(serviceProvider);

        // Assert
        instance.Single.ShouldNotBeNull();
        instance.Single.Value.ShouldBe(42);
        instance.IsSingle.ShouldBeTrue();
        instance.IsMultiply.ShouldBeFalse();
        instance.UseInstance.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithMultipleServices_ShouldSetMultiplyProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new TestService { Value = 1 });
        services.AddSingleton(new TestService { Value = 2 });
        services.AddSingleton(new TestService { Value = 3 });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var instance = new MultiplyServiceInstance<TestService>(serviceProvider);

        // Assert
        instance.Single.ShouldBeNull();
        instance.Multiply.ShouldNotBeNull();
        instance.Multiply.Length.ShouldBe(3);
        instance.IsSingle.ShouldBeFalse();
        instance.IsMultiply.ShouldBeTrue();
        instance.UseInstance.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithNoServices_ShouldSetMultiplyPropertyToEmptyArray()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var instance = new MultiplyServiceInstance<TestService>(serviceProvider);

        // Assert
        instance.Single.ShouldBeNull();
        instance.Multiply.ShouldNotBeNull();
        instance.Multiply.Length.ShouldBe(0);
        instance.IsSingle.ShouldBeFalse();
        instance.IsMultiply.ShouldBeTrue();
        instance.UseInstance.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithEnumerable_ShouldCreateMultiplyInstance()
    {
        // Arrange
        var enumerable = new List<TestService>
        {
            new() { Value = 1 },
            new() { Value = 2 }
        };

        // Act
        var instance = new MultiplyServiceInstance<TestService>(enumerable);

        // Assert
        instance.Single.ShouldBeNull();
        instance.Multiply.ShouldNotBeNull();
        instance.Multiply.Length.ShouldBe(2);
        instance.IsMultiply.ShouldBeTrue();
        instance.UseInstance.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithSingleItemEnumerable_ShouldSetSingleProperty()
    {
        // Arrange
        var enumerable = new List<TestService>
        {
            new() { Value = 42 }
        };

        // Act
        var instance = new MultiplyServiceInstance<TestService>(enumerable);

        // Assert
        instance.Single.ShouldNotBeNull();
        instance.Single.Value.ShouldBe(42);
        instance.IsSingle.ShouldBeTrue();
        instance.UseInstance.ShouldBeTrue();
    }

    [Fact]
    public void Nullable_ShouldReturnInstanceWithNoServices()
    {
        // Act
        var instance = MultiplyServiceInstance<TestService>.Nullable;

        // Assert
        instance.Single.ShouldBeNull();
        instance.Multiply.ShouldBeNull();
        instance.IsSingle.ShouldBeFalse();
        instance.IsMultiply.ShouldBeFalse();
        instance.UseInstance.ShouldBeFalse();
    }

    [Fact]
    public async Task CheckoutInstanceAsync_WithSingleInstance_ShouldCallSingleAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedService = new TestService { Value = 42 };
        services.AddSingleton(expectedService);
        var serviceProvider = services.BuildServiceProvider();
        var instance = new MultiplyServiceInstance<TestService>(serviceProvider);

        var singleActionCalled = false;
        var multiplyActionCalled = false;

        // Act
        await instance.CheckoutInstanceAsync(
            singleAction: service =>
            {
                singleActionCalled = true;
                service.Value.ShouldBe(42);
                return ValueTask.CompletedTask;
            },
            multiplyAction: _ =>
            {
                multiplyActionCalled = true;
                return ValueTask.CompletedTask;
            });

        // Assert
        singleActionCalled.ShouldBeTrue();
        multiplyActionCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task CheckoutInstanceAsync_WithMultipleInstances_ShouldCallMultiplyAction()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new TestService { Value = 1 });
        services.AddSingleton(new TestService { Value = 2 });
        var serviceProvider = services.BuildServiceProvider();
        var instance = new MultiplyServiceInstance<TestService>(serviceProvider);

        var singleActionCalled = false;
        var multiplyActionCalled = false;
        var processedValues = new List<int>();

        // Act
        await instance.CheckoutInstanceAsync(
            singleAction: _ =>
            {
                singleActionCalled = true;
                return ValueTask.CompletedTask;
            },
            multiplyAction: services =>
            {
                multiplyActionCalled = true;
                foreach (var service in services)
                {
                    processedValues.Add(service.Value);
                }
                return ValueTask.CompletedTask;
            });

        // Assert
        singleActionCalled.ShouldBeFalse();
        multiplyActionCalled.ShouldBeTrue();
        processedValues.ShouldContain(1);
        processedValues.ShouldContain(2);
    }

    [Fact]
    public async Task CheckoutInstanceAsync_WithNoInstances_ShouldThrowEmptyInstanceBranchException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var instance = new MultiplyServiceInstance<TestService>(serviceProvider);

        // Act & Assert
        var exception = await Should.ThrowAsync<EmptyInstanceBranchException>(async () =>
        {
            await instance.CheckoutInstanceAsync(
                singleAction: _ => ValueTask.CompletedTask,
                multiplyAction: _ => ValueTask.CompletedTask);
        });

        exception.Message.ShouldContain(typeof(TestService).ToString());
    }

    [Fact]
    public async Task CheckoutInstanceAsync_Generic_WithSingleInstance_ShouldCallSingleAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedService = new TestService { Value = 42 };
        services.AddSingleton(expectedService);
        var serviceProvider = services.BuildServiceProvider();
        var instance = new MultiplyServiceInstance<TestService>(serviceProvider);

        var singleActionCalled = false;
        var multiplyActionCalled = false;

        // Act
        var result = await instance.CheckoutInstanceAsync(
            singleAction: service =>
            {
                singleActionCalled = true;
                service.Value.ShouldBe(42);
                return ValueTask.FromResult("single");
            },
            multiplyAction: _ =>
            {
                multiplyActionCalled = true;
                return ValueTask.FromResult("multiply");
            });

        // Assert
        result.ShouldBe("single");
        singleActionCalled.ShouldBeTrue();
        multiplyActionCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task CheckoutInstanceAsync_Generic_WithMultipleInstances_ShouldCallMultiplyAction()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new TestService { Value = 1 });
        services.AddSingleton(new TestService { Value = 2 });
        var serviceProvider = services.BuildServiceProvider();
        var instance = new MultiplyServiceInstance<TestService>(serviceProvider);

        var singleActionCalled = false;
        var multiplyActionCalled = false;
        var processedCount = 0;

        // Act
        var result = await instance.CheckoutInstanceAsync(
            singleAction: _ =>
            {
                singleActionCalled = true;
                return ValueTask.FromResult("single");
            },
            multiplyAction: services =>
            {
                multiplyActionCalled = true;
                foreach (var service in services)
                {
                    processedCount++;
                }
                return ValueTask.FromResult($"multiply-{processedCount}");
            });

        // Assert
        result.ShouldBe("multiply-2");
        singleActionCalled.ShouldBeFalse();
        multiplyActionCalled.ShouldBeTrue();
        processedCount.ShouldBe(2);
    }

    [Fact]
    public async Task CheckoutInstanceAsync_Generic_WithNoInstances_ShouldThrowEmptyInstanceBranchException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var instance = new MultiplyServiceInstance<TestService>(serviceProvider);

        // Act & Assert
        var exception = await Should.ThrowAsync<EmptyInstanceBranchException>(async () =>
        {
            await instance.CheckoutInstanceAsync(
                singleAction: _ => ValueTask.FromResult(0),
                multiplyAction: _ => ValueTask.FromResult(0));
        });

        exception.Message.ShouldContain(typeof(TestService).ToString());
    }
}
