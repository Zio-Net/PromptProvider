using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PromptProvider.Interfaces;
using PromptProvider.Models;
using PromptProvider.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PromptProvider.Options;
using Xunit;

namespace PromptProvider.Tests;

public class PromptServiceTests
{
    [Fact]
    public async Task GetPromptAsync_FallbacksToDefaults_WhenLangfuseUnavailable()
    {
        var langfuseMock = new Mock<ILangfuseService>();
        langfuseMock.Setup(l => l.GetPromptAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not configured"));

        var defaultsProviderMock = new Mock<IDefaultPromptsProvider>();
        defaultsProviderMock.Setup(p => p.GetDefaults()).Returns(new System.Collections.Generic.Dictionary<string, string>
        {
            { "WelcomePrompt", "Welcome!" }
        });

        var langfuseOptions = Options.Create(new LangfuseOptions());

        var service = new PromptService(
            new NullLogger<PromptService>(),
            langfuseMock.Object,
            defaultsProviderMock.Object,
            langfuseOptions
        );

        var result = await service.GetPromptAsync("WelcomePrompt");

        Assert.NotNull(result);
        Assert.Equal("WelcomePrompt", result!.PromptKey);
        Assert.Equal("Welcome!", result.Content);
        Assert.Equal("Local", result.Source);
    }

    [Fact]
    public async Task GetPromptAsync_ReturnsLangfuseResult_WhenAvailable()
    {
        var langfuseMock = new Mock<ILangfuseService>();
        langfuseMock.Setup(l => l.GetPromptAsync("Key1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LangfusePromptModel { Name = "Key1", Prompt = "From Langfuse", Version = 1, Type = "text", Labels = Array.Empty<string>(), Tags = Array.Empty<string>() });

        var defaultsProviderMock = new Mock<IDefaultPromptsProvider>();
        defaultsProviderMock.Setup(p => p.GetDefaults()).Returns(new System.Collections.Generic.Dictionary<string, string>());

        var langfuseOptions = Options.Create(new LangfuseOptions { BaseUrl = "https://x", PublicKey = "a", SecretKey = "b" });

        var service = new PromptService(
            new NullLogger<PromptService>(),
            langfuseMock.Object,
            defaultsProviderMock.Object,
            langfuseOptions
        );

        var result = await service.GetPromptAsync("Key1");

        Assert.NotNull(result);
        Assert.Equal("Key1", result!.PromptKey);
        Assert.Equal("From Langfuse", result.Content);
        Assert.Equal("Langfuse", result.Source);
    }
}
