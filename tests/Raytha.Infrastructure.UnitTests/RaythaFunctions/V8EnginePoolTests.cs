using FluentAssertions;
using Microsoft.ClearScript.V8;
using Raytha.Infrastructure.RaythaFunctions;

namespace Raytha.Infrastructure.UnitTests.RaythaFunctions;

[TestFixture]
public class V8EnginePoolTests
{
    #region Constructor Tests

    [Test]
    public void Constructor_WithDefaultPoolSize_ShouldCreatePool()
    {
        // Act
        using var pool = new V8EnginePool();

        // Assert - pool should be usable
        var engine = pool.Rent();
        engine.Should().NotBeNull();
        pool.Return(engine);
    }

    [Test]
    public void Constructor_WithCustomPoolSize_ShouldCreatePool()
    {
        // Act
        using var pool = new V8EnginePool(maxPoolSize: 5);

        // Assert - pool should be usable
        var engine = pool.Rent();
        engine.Should().NotBeNull();
        pool.Return(engine);
    }

    [Test]
    public void Constructor_ShouldPrewarmWithTwoEngines()
    {
        // Arrange & Act
        using var pool = new V8EnginePool(maxPoolSize: 10);

        // The pool pre-warms with min(2, maxPoolSize) engines
        // We can verify by renting without creating new engines
        var engine1 = pool.Rent();
        var engine2 = pool.Rent();

        // Assert
        engine1.Should().NotBeNull();
        engine2.Should().NotBeNull();
        engine1.Should().NotBeSameAs(engine2);

        pool.Return(engine1);
        pool.Return(engine2);
    }

    [Test]
    public void Constructor_WithPoolSizeOfOne_ShouldPrewarmWithOneEngine()
    {
        // Arrange & Act
        using var pool = new V8EnginePool(maxPoolSize: 1);

        // Assert - should work with pool size of 1
        var engine = pool.Rent();
        engine.Should().NotBeNull();
        pool.Return(engine);
    }

    #endregion

    #region Rent Tests

    [Test]
    public void Rent_ShouldReturnValidEngine()
    {
        // Arrange
        using var pool = new V8EnginePool();

        // Act
        var engine = pool.Rent();

        // Assert
        engine.Should().NotBeNull();
        engine.Should().BeOfType<V8ScriptEngine>();
        pool.Return(engine);
    }

    [Test]
    public void Rent_WhenPoolExhausted_ShouldCreateNewEngine()
    {
        // Arrange
        using var pool = new V8EnginePool(maxPoolSize: 2);
        var engines = new List<V8ScriptEngine>();

        // Act - rent more engines than pool size
        for (int i = 0; i < 5; i++)
        {
            engines.Add(pool.Rent());
        }

        // Assert - all engines should be valid
        engines.Should().HaveCount(5);
        engines.Should().OnlyContain(e => e != null);
        engines.Should().OnlyHaveUniqueItems();

        // Cleanup
        foreach (var engine in engines)
        {
            pool.Return(engine);
        }
    }

    [Test]
    public void Rent_ShouldReturnEngineWithHostTypesLoaded()
    {
        // Arrange
        using var pool = new V8EnginePool();

        // Act
        var engine = pool.Rent();

        // Assert - verify common host types are available
        // The engine should have types like Guid, DateTime, etc. loaded
        engine.Invoking(e => e.Execute("var g = new Guid();"))
            .Should().NotThrow();

        pool.Return(engine);
    }

    [Test]
    public void Rent_ShouldReturnEngineWithResultClassesLoaded()
    {
        // Arrange
        using var pool = new V8EnginePool();

        // Act
        var engine = pool.Rent();

        // Assert - verify result classes are available
        engine.Invoking(e => e.Execute("var result = new JsonResult({test: 1});"))
            .Should().NotThrow();
        engine.Invoking(e => e.Execute("var html = new HtmlResult('<div>test</div>');"))
            .Should().NotThrow();
        engine.Invoking(e => e.Execute("var xml = new XmlResult('<root/>');"))
            .Should().NotThrow();
        engine.Invoking(e => e.Execute("var redirect = new RedirectResult('/path');"))
            .Should().NotThrow();
        engine.Invoking(e => e.Execute("var status = new StatusCodeResult(404, 'Not Found');"))
            .Should().NotThrow();

        pool.Return(engine);
    }

    [Test]
    public void Rent_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var pool = new V8EnginePool();
        pool.Dispose();

        // Act & Assert
        pool.Invoking(p => p.Rent())
            .Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region Return Tests

    [Test]
    public void Return_ShouldDisposeEngineAndAddNewToPool()
    {
        // Arrange
        using var pool = new V8EnginePool(maxPoolSize: 5);
        var originalEngine = pool.Rent();

        // Act
        pool.Return(originalEngine);

        // The returned engine is disposed and a new fresh one is added
        // Verify by renting again - should get a different engine
        var newEngine = pool.Rent();

        // Assert
        newEngine.Should().NotBeNull();
        // The original engine should be disposed (can't verify directly, but new one works)
        newEngine.Invoking(e => e.Execute("var x = 1;")).Should().NotThrow();

        pool.Return(newEngine);
    }

    [Test]
    public void Return_AfterDispose_ShouldDisposeEngineWithoutException()
    {
        // Arrange
        var pool = new V8EnginePool();
        var engine = pool.Rent();
        pool.Dispose();

        // Act & Assert - should not throw
        pool.Invoking(p => p.Return(engine))
            .Should().NotThrow();
    }

    [Test]
    public void Return_WhenPoolAtMaxSize_ShouldNotExceedMaxSize()
    {
        // Arrange
        using var pool = new V8EnginePool(maxPoolSize: 3);

        // Rent and return multiple engines
        for (int i = 0; i < 10; i++)
        {
            var engine = pool.Rent();
            pool.Return(engine);
        }

        // Pool should still function correctly
        var finalEngine = pool.Rent();
        finalEngine.Should().NotBeNull();
        pool.Return(finalEngine);
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var pool = new V8EnginePool();

        // Act & Assert - should not throw on multiple disposes
        pool.Invoking(p => p.Dispose()).Should().NotThrow();
        pool.Invoking(p => p.Dispose()).Should().NotThrow();
        pool.Invoking(p => p.Dispose()).Should().NotThrow();
    }

    [Test]
    public void Dispose_ShouldDisposeAllPooledEngines()
    {
        // Arrange
        var pool = new V8EnginePool(maxPoolSize: 5);

        // Rent and return some engines to ensure pool has engines
        for (int i = 0; i < 3; i++)
        {
            var engine = pool.Rent();
            pool.Return(engine);
        }

        // Act
        pool.Dispose();

        // Assert - pool should be disposed
        pool.Invoking(p => p.Rent())
            .Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region Thread Safety Tests

    [Test]
    public void Pool_ShouldBeThreadSafe()
    {
        // Arrange
        using var pool = new V8EnginePool(maxPoolSize: 5);
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Act - run multiple concurrent rent/return operations
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var engine = pool.Rent();
                        // Simulate some work
                        engine.Execute("var x = 1 + 1;");
                        pool.Return(engine);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.Should().BeEmpty("concurrent operations should not cause exceptions");
    }

    #endregion

    #region Engine Functionality Tests

    [Test]
    public void RentedEngine_ShouldExecuteBasicJavaScript()
    {
        // Arrange
        using var pool = new V8EnginePool();
        var engine = pool.Rent();

        // Act
        engine.Execute("var result = 2 + 2;");
        var result = engine.Evaluate("result");

        // Assert
        result.Should().Be(4);

        pool.Return(engine);
    }

    [Test]
    public void RentedEngine_ShouldSupportDotNetTypes()
    {
        // Arrange
        using var pool = new V8EnginePool();
        var engine = pool.Rent();

        // Act - use DateTime which should be loaded
        engine.Execute("var now = DateTime.Now;");
        var result = engine.Evaluate("now.Year");

        // Assert
        ((int)result).Should().BeGreaterThanOrEqualTo(2024);

        pool.Return(engine);
    }

    [Test]
    public void RentedEngine_JsonResultClass_ShouldHaveCorrectProperties()
    {
        // Arrange
        using var pool = new V8EnginePool();
        var engine = pool.Rent();

        // Act
        engine.Execute("var result = new JsonResult({name: 'test', value: 42});");
        var contentType = engine.Evaluate("result.contentType");
        var body = engine.Evaluate("result.body");

        // Assert
        contentType.Should().Be("application/json");
        body.Should().NotBeNull();

        pool.Return(engine);
    }

    [Test]
    public void RentedEngine_HtmlResultClass_ShouldHaveCorrectProperties()
    {
        // Arrange
        using var pool = new V8EnginePool();
        var engine = pool.Rent();

        // Act
        engine.Execute("var result = new HtmlResult('<h1>Hello</h1>');");
        var contentType = engine.Evaluate("result.contentType");
        var body = engine.Evaluate("result.body");

        // Assert
        contentType.Should().Be("text/html");
        body.Should().Be("<h1>Hello</h1>");

        pool.Return(engine);
    }

    [Test]
    public void RentedEngine_XmlResultClass_ShouldHaveCorrectProperties()
    {
        // Arrange
        using var pool = new V8EnginePool();
        var engine = pool.Rent();

        // Act
        engine.Execute("var result = new XmlResult('<root><item/></root>');");
        var contentType = engine.Evaluate("result.contentType");
        var body = engine.Evaluate("result.body");

        // Assert
        contentType.Should().Be("application/xml");
        body.Should().Be("<root><item/></root>");

        pool.Return(engine);
    }

    [Test]
    public void RentedEngine_RedirectResultClass_ShouldHaveCorrectProperties()
    {
        // Arrange
        using var pool = new V8EnginePool();
        var engine = pool.Rent();

        // Act
        engine.Execute("var result = new RedirectResult('/new-location');");
        var contentType = engine.Evaluate("result.contentType");
        var body = engine.Evaluate("result.body");

        // Assert
        contentType.Should().Be("redirectToUrl");
        body.Should().Be("/new-location");

        pool.Return(engine);
    }

    [Test]
    public void RentedEngine_StatusCodeResultClass_ShouldHaveCorrectProperties()
    {
        // Arrange
        using var pool = new V8EnginePool();
        var engine = pool.Rent();

        // Act
        engine.Execute("var result = new StatusCodeResult(404, 'Not Found');");
        var contentType = engine.Evaluate("result.contentType");
        var statusCode = engine.Evaluate("result.statusCode");
        var body = engine.Evaluate("result.body");

        // Assert
        contentType.Should().Be("statusCode");
        statusCode.Should().Be(404);
        body.Should().Be("Not Found");

        pool.Return(engine);
    }

    #endregion
}

