using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;
using Mintlify.Core.Models.Integrations;

namespace Mintlify.Tests.Core.Models
{

    /// <summary>
    /// Tests for new configuration model classes added in the schema gap work.
    /// Verifies serialization round-trips for all properties on each config type.
    /// </summary>
    [TestClass]
    public class NewConfigTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region MetadataConfig Tests

        /// <summary>
        /// Tests that MetadataConfig serializes and deserializes the Timestamp property.
        /// </summary>
        [TestMethod]
        public void MetadataConfig_Serialize_IncludesTimestamp()
        {
            var config = new MetadataConfig { Timestamp = true };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<MetadataConfig>(json, _jsonOptions);

            json.Should().Contain("\"timestamp\": true");
            result.Should().NotBeNull();
            result!.Timestamp.Should().BeTrue();
        }

        #endregion

        #region ThumbnailsConfig Tests

        /// <summary>
        /// Tests that ThumbnailsConfig serializes and deserializes all properties.
        /// </summary>
        [TestMethod]
        public void ThumbnailsConfig_Serialize_IncludesAllProperties()
        {
            var config = new ThumbnailsConfig
            {
                Appearance = "dark",
                Background = "/images/thumb-bg.png"
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<ThumbnailsConfig>(json, _jsonOptions);

            json.Should().Contain("\"appearance\": \"dark\"");
            json.Should().Contain("\"background\": \"/images/thumb-bg.png\"");
            result.Should().NotBeNull();
            result!.Appearance.Should().Be("dark");
            result!.Background.Should().Be("/images/thumb-bg.png");
        }

        #endregion

        #region StylingConfig Tests

        /// <summary>
        /// Tests that StylingConfig serializes the Latex property correctly.
        /// </summary>
        [TestMethod]
        public void StylingConfig_Serialize_IncludesLatex()
        {
            var config = new StylingConfig
            {
                Latex = true,
                Codeblocks = "dark",
                Eyebrows = "breadcrumbs"
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<StylingConfig>(json, _jsonOptions);

            json.Should().Contain("\"latex\": true");
            json.Should().Contain("\"codeblocks\": \"dark\"");
            result.Should().NotBeNull();
            result!.Latex.Should().BeTrue();
            result!.Codeblocks.Should().Be("dark");
            result!.Eyebrows.Should().Be("breadcrumbs");
        }

        #endregion

        #region Error404Config Tests

        /// <summary>
        /// Tests that Error404Config serializes Title and Description properties.
        /// </summary>
        [TestMethod]
        public void Error404Config_Serialize_IncludesTitleAndDescription()
        {
            var config = new Error404Config
            {
                Title = "Oops!",
                Description = "Page not found",
                Redirect = false
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<Error404Config>(json, _jsonOptions);

            json.Should().Contain("\"title\": \"Oops!\"");
            json.Should().Contain("\"description\": \"Page not found\"");
            result.Should().NotBeNull();
            result!.Title.Should().Be("Oops!");
            result!.Description.Should().Be("Page not found");
            result!.Redirect.Should().BeFalse();
        }

        #endregion

        #region ContextualConfig Tests

        /// <summary>
        /// Tests that ContextualConfig serializes the Display property.
        /// </summary>
        [TestMethod]
        public void ContextualConfig_Serialize_IncludesDisplay()
        {
            var config = new ContextualConfig
            {
                Display = "toc",
                Options = new List<string> { "copy", "claude" }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<ContextualConfig>(json, _jsonOptions);

            json.Should().Contain("\"display\": \"toc\"");
            result.Should().NotBeNull();
            result!.Display.Should().Be("toc");
            result!.Options.Should().Contain("copy");
            result!.Options.Should().Contain("claude");
        }

        #endregion

        #region ApiExamplesConfig Tests

        /// <summary>
        /// Tests that ApiExamplesConfig serializes the Autogenerate property.
        /// </summary>
        [TestMethod]
        public void ApiExamplesConfig_Serialize_IncludesAutogenerate()
        {
            var config = new ApiExamplesConfig
            {
                Autogenerate = true,
                Prefill = false,
                Defaults = "required",
                Languages = new List<string> { "python", "curl" }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<ApiExamplesConfig>(json, _jsonOptions);

            json.Should().Contain("\"autogenerate\": true");
            json.Should().Contain("\"prefill\": false");
            json.Should().Contain("\"defaults\": \"required\"");
            result.Should().NotBeNull();
            result!.Autogenerate.Should().BeTrue();
            result!.Prefill.Should().BeFalse();
            result!.Languages.Should().Contain("python");
        }

        #endregion

        #region PostHogConfig Tests

        /// <summary>
        /// Tests that PostHogConfig serializes the SessionRecording property.
        /// </summary>
        [TestMethod]
        public void PostHogConfig_Serialize_IncludesSessionRecording()
        {
            var config = new PostHogConfig
            {
                ApiKey = "phc_test123",
                ApiHost = "https://posthog.example.com",
                SessionRecording = false
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<PostHogConfig>(json, _jsonOptions);

            json.Should().Contain("\"sessionRecording\": false");
            result.Should().NotBeNull();
            result!.ApiKey.Should().Be("phc_test123");
            result!.ApiHost.Should().Be("https://posthog.example.com");
            result!.SessionRecording.Should().BeFalse();
        }

        #endregion

        #region FontConfig Tests

        /// <summary>
        /// Tests that FontConfig serializes all properties.
        /// </summary>
        [TestMethod]
        public void FontConfig_Serialize_IncludesAllProperties()
        {
            var config = new FontConfig
            {
                Family = "Inter",
                Weight = 400,
                Source = "https://fonts.example.com/inter.woff2",
                Format = "woff2"
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<FontConfig>(json, _jsonOptions);

            json.Should().Contain("\"family\": \"Inter\"");
            json.Should().Contain("\"weight\": 400");
            json.Should().Contain("\"format\": \"woff2\"");
            result.Should().NotBeNull();
            result!.Family.Should().Be("Inter");
            result!.Weight.Should().Be(400);
            result!.Source.Should().Be("https://fonts.example.com/inter.woff2");
            result!.Format.Should().Be("woff2");
        }

        #endregion

        #region Integration Config Tests

        /// <summary>
        /// Tests that AdobeConfig serializes the LaunchUrl property.
        /// </summary>
        [TestMethod]
        public void AdobeConfig_Serialize_IncludesLaunchUrl()
        {
            var config = new AdobeConfig { LaunchUrl = "https://adobe.launch.example.com" };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<AdobeConfig>(json, _jsonOptions);

            json.Should().Contain("\"launchUrl\"");
            result.Should().NotBeNull();
            result!.LaunchUrl.Should().Be("https://adobe.launch.example.com");
        }

        /// <summary>
        /// Tests that ClarityConfig serializes the ProjectId property.
        /// </summary>
        [TestMethod]
        public void ClarityConfig_Serialize_IncludesProjectId()
        {
            var config = new ClarityConfig { ProjectId = "clarity-123" };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<ClarityConfig>(json, _jsonOptions);

            json.Should().Contain("\"projectId\"");
            result.Should().NotBeNull();
            result!.ProjectId.Should().Be("clarity-123");
        }

        /// <summary>
        /// Tests that CookiesConfig serializes Key and Value properties.
        /// </summary>
        [TestMethod]
        public void CookiesConfig_Serialize_IncludesKeyAndValue()
        {
            var config = new CookiesConfig { Key = "session", Value = "abc123" };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<CookiesConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Key.Should().Be("session");
            result!.Value.Should().Be("abc123");
        }

        /// <summary>
        /// Tests that FrontChatConfig serializes the SnippetId property.
        /// </summary>
        [TestMethod]
        public void FrontChatConfig_Serialize_IncludesSnippetId()
        {
            var config = new FrontChatConfig { SnippetId = "fc-snippet-1" };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<FrontChatConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.SnippetId.Should().Be("fc-snippet-1");
        }

        /// <summary>
        /// Tests that IntercomConfig serializes the AppId property.
        /// </summary>
        [TestMethod]
        public void IntercomConfig_Serialize_IncludesAppId()
        {
            var config = new IntercomConfig { AppId = "ic-app-1" };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<IntercomConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.AppId.Should().Be("ic-app-1");
        }

        /// <summary>
        /// Tests that KoalaConfig serializes the PublicApiKey property.
        /// </summary>
        [TestMethod]
        public void KoalaConfig_Serialize_IncludesPublicApiKey()
        {
            var config = new KoalaConfig { PublicApiKey = "koala-key-1" };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<KoalaConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.PublicApiKey.Should().Be("koala-key-1");
        }

        /// <summary>
        /// Tests that TelemetryConfig serializes the Enabled property.
        /// </summary>
        [TestMethod]
        public void TelemetryConfig_Serialize_IncludesEnabled()
        {
            var config = new TelemetryConfig { Enabled = true };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<TelemetryConfig>(json, _jsonOptions);

            json.Should().Contain("\"enabled\": true");
            result.Should().NotBeNull();
            result!.Enabled.Should().BeTrue();
        }

        #endregion

        #region NavbarLink Tests

        /// <summary>
        /// Tests that NavbarLink serializes the Type property.
        /// </summary>
        [TestMethod]
        public void NavbarLink_Serialize_IncludesType()
        {
            var link = new NavbarLink
            {
                Label = "GitHub",
                Href = "https://github.com/example",
                Type = "github"
            };

            var json = JsonSerializer.Serialize(link, _jsonOptions);
            var result = JsonSerializer.Deserialize<NavbarLink>(json, _jsonOptions);

            json.Should().Contain("\"type\": \"github\"");
            result.Should().NotBeNull();
            result!.Type.Should().Be("github");
            result!.Label.Should().Be("GitHub");
            result!.Href.Should().Be("https://github.com/example");
        }

        #endregion

        #region ApiConfig Tests

        /// <summary>
        /// Tests that ApiConfig serializes the Url property.
        /// </summary>
        [TestMethod]
        public void ApiConfig_Serialize_IncludesUrl()
        {
            var config = new ApiConfig
            {
                Url = "https://api.example.com",
                Proxy = true
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<ApiConfig>(json, _jsonOptions);

            json.Should().Contain("\"url\": \"https://api.example.com\"");
            result.Should().NotBeNull();
            result!.Url.Should().Be("https://api.example.com");
            result!.Proxy.Should().BeTrue();
        }

        #endregion

    }

}
