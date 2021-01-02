﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Text.Json;
using Yuniql.Core;
using Yuniql.Extensibility;

namespace Yuniql.UnitTests
{
    [TestClass]
    public class ConfigurationServiceTests: TestClassBase
    {
        private Configuration GetFreshConfiguration()
        {
            var configuration = Configuration.Instance;
            configuration.WorkspacePath = @"c:\temp\yuniql";
            configuration.DebugTraceMode = true;
            configuration.Platform = SUPPORTED_DATABASES.SQLSERVER;
            configuration.ConnectionString = "sqlserver-connectionstring";
            configuration.CommandTimeout = 30;
            configuration.TargetVersion = "v0.00";
            configuration.AutoCreateDatabase = true;
            configuration.Tokens = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string> ("token1", "value1"),
                new KeyValuePair<string, string> ("token2", "value2"),
                new KeyValuePair<string, string> ("token3", "value3")
            };
            configuration.BulkSeparator = ";";
            configuration.BulkBatchSize = 1000;
            configuration.Environment = "dev";
            configuration.MetaSchemaName = "yuniql_schema";
            configuration.MetaTableName = "yuniql_table";
            configuration.TransactionMode = TRANSACTION_MODE.SESSION;
            configuration.ContinueAfterFailure = true;
            configuration.RequiredClearedDraft = true;
            configuration.IsForced = true;
            configuration.VerifyOnly = true;
            configuration.AppliedByTool = "yuniql-cli";
            configuration.AppliedByToolVersion = "v1.0.0.0";

            return configuration;
    }

        [TestMethod]
        public void Test_Paremeters_Mapped_To_Configuration()
        {
            //arrange
            var traceService = new Mock<ITraceService>();
            var localVersionService = new Mock<ILocalVersionService>();

            var parameters = GetFreshConfiguration();
            var environmentService = new Mock<IEnvironmentService>();
            environmentService.Setup(s => s.GetEnvironmentVariable(ENVIRONMENT_VARIABLE.YUNIQL_WORKSPACE)).Returns(parameters.WorkspacePath);
            environmentService.Setup(s => s.GetEnvironmentVariable(ENVIRONMENT_VARIABLE.YUNIQL_TARGET_PLATFORM)).Returns(parameters.Platform);
            environmentService.Setup(s => s.GetEnvironmentVariable(ENVIRONMENT_VARIABLE.YUNIQL_CONNECTION_STRING)).Returns(parameters.ConnectionString);

            //act
            var sut = new ConfigurationService(environmentService.Object, localVersionService.Object, traceService.Object);
            sut.Initialize();
            var configuration = sut.GetConfiguration();

            //assert
            var parameterJson = JsonSerializer.Serialize(parameters, new JsonSerializerOptions { WriteIndented = true, IgnoreReadOnlyProperties = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var configurationJson = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true, IgnoreReadOnlyProperties = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            parameterJson.ShouldBe(configurationJson);
        }

        [TestMethod]
        public void Test_Print_Redaction_Enabled()
        {
            //arrange
            var traceService = new Mock<ITraceService>();
            var localVersionService = new Mock<ILocalVersionService>();

            var parameters = GetFreshConfiguration();
            var environmentService = new Mock<IEnvironmentService>();
            environmentService.Setup(s => s.GetEnvironmentVariable(ENVIRONMENT_VARIABLE.YUNIQL_WORKSPACE)).Returns(parameters.WorkspacePath);
            environmentService.Setup(s => s.GetEnvironmentVariable(ENVIRONMENT_VARIABLE.YUNIQL_TARGET_PLATFORM)).Returns(parameters.Platform);
            environmentService.Setup(s => s.GetEnvironmentVariable(ENVIRONMENT_VARIABLE.YUNIQL_CONNECTION_STRING)).Returns(parameters.ConnectionString);

            //act
            var sut = new ConfigurationService(environmentService.Object, localVersionService.Object, traceService.Object);
            sut.Initialize();
            var configurationJson = sut.PrintAsJson(redactSensitiveText: true);

            //assert
            var configuration = JsonSerializer.Deserialize<Configuration>(configurationJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            configuration.ConnectionString.ShouldBe("<sensitive-data-redacted>");
        }

        [TestMethod]
        public void Test_Print_Redaction_Disabled()
        {
            //arrange
            var traceService = new Mock<ITraceService>();
            var localVersionService = new Mock<ILocalVersionService>();

            var parameters = GetFreshConfiguration();
            var environmentService = new Mock<IEnvironmentService>();
            environmentService.Setup(s => s.GetEnvironmentVariable(ENVIRONMENT_VARIABLE.YUNIQL_WORKSPACE)).Returns(parameters.WorkspacePath);
            environmentService.Setup(s => s.GetEnvironmentVariable(ENVIRONMENT_VARIABLE.YUNIQL_TARGET_PLATFORM)).Returns(parameters.Platform);
            environmentService.Setup(s => s.GetEnvironmentVariable(ENVIRONMENT_VARIABLE.YUNIQL_CONNECTION_STRING)).Returns(parameters.ConnectionString);

            //act
            var sut = new ConfigurationService(environmentService.Object, localVersionService.Object, traceService.Object);
            sut.Initialize();
            var configurationJson = sut.PrintAsJson(redactSensitiveText: false);

            //assert
            var configuration = JsonSerializer.Deserialize<Configuration>(configurationJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            configuration.ConnectionString.ShouldBe(parameters.ConnectionString);
        }

        [TestMethod]
        public void Test_GetValueOrDefault_With_Use_Original_Value()
        {
            //arrange
            var traceService = new Mock<ITraceService>();
            var environmentService = new Mock<IEnvironmentService>();
            var localVersionService = new Mock<ILocalVersionService>();

            //act
            var sut = new ConfigurationService(environmentService.Object, localVersionService.Object, traceService.Object);
            var result = sut.GetValueOrDefault("mariadb", ENVIRONMENT_VARIABLE.YUNIQL_TARGET_PLATFORM, SUPPORTED_DATABASES.SQLSERVER);

            //assert
            result.ShouldBe("mariadb");
        }

        [TestMethod]
        public void Test_GetValueOrDefault_Use_Environment_Variable()
        {
            //arrange
            var traceService = new Mock<ITraceService>();
            var environmentService = new Mock<IEnvironmentService>();
            environmentService.Setup(s => s.GetEnvironmentVariable(ENVIRONMENT_VARIABLE.YUNIQL_TARGET_PLATFORM)).Returns(SUPPORTED_DATABASES.MARIADB);
            var localVersionService = new Mock<ILocalVersionService>();

            //act
            var sut = new ConfigurationService(environmentService.Object, localVersionService.Object, traceService.Object);
            var result = sut.GetValueOrDefault(null, ENVIRONMENT_VARIABLE.YUNIQL_TARGET_PLATFORM, SUPPORTED_DATABASES.SQLSERVER);

            //assert
            result.ShouldBe(SUPPORTED_DATABASES.MARIADB);
        }

        [TestMethod]
        public void Test_GetValueOrDefault_Use_Default_Value()
        {
            //arrange
            var traceService = new Mock<ITraceService>();
            var environmentService = new Mock<IEnvironmentService>();
            var localVersionService = new Mock<ILocalVersionService>();

            //act
            var sut = new ConfigurationService(environmentService.Object, localVersionService.Object, traceService.Object);
            var result = sut.GetValueOrDefault(null, ENVIRONMENT_VARIABLE.YUNIQL_TARGET_PLATFORM, SUPPORTED_DATABASES.SQLSERVER);

            //assert
            result.ShouldBe(SUPPORTED_DATABASES.SQLSERVER);
        }

    }

}