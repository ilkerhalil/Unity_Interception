using System;
using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling.Logging;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling.PolicyInjection;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.PolicyInjection;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.InterceptionExtension;
using Unity_Interception.Properties;

namespace Unity_Interception
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = BuildContainer();
            var smsProvider = container.Resolve<ISmsProvider>();
            var result = smsProvider.SendSms(new SmsRequest("5422476935", "Merhaba"));
            Console.WriteLine(result.Status);
        }

        private static UnityContainer BuildContainer()
        {
            var configurationSourceBuilder = new ConfigurationSourceBuilder();
            var configurationSource = new DictionaryConfigurationSource();
            DefineLogger(configurationSourceBuilder,configurationSource);
            DefineExceptionHandling(configurationSourceBuilder,configurationSource);
            var container = new UnityContainer();
            container.AddNewExtension<Interception>();
            container.RegisterType<ISmsProvider, SmsProvider>(new InterceptionBehavior<PolicyInjectionBehavior>()
                , new Interceptor<TransparentProxyInterceptor>())
                .Configure<Interception>()
                .AddPolicy(Settings.Default.UnityPolicyName)
                .AddMatchingRule<TypeMatchingRule>(
                    new InjectionConstructor(new InjectionParameter(typeof(ISmsProvider))))
                .AddCallHandler<ExceptionCallHandler>(new InjectionConstructor(Settings.Default.UnityPolicyName))
                .AddCallHandler<LogCallHandler>(new InjectionConstructor(Settings.Default.LoggingHandlerEventId
                    , Settings.Default.LoggingHandlerLogBeforeCall
                    , Settings.Default.LoggingHandlerLogAfterCall
                    , Settings.Default.LoggingHandlerBeforeMessage
                    , Settings.Default.LoggingHandlerAfterMessage
                    , Settings.Default.LoggingHandlerIncludeParameters
                    , Settings.Default.LoggingHandlerIncludeCallStack
                    , Settings.Default.LoggingHandlerIncludeCallTime
                    , Settings.Default.LoggingHandlerPriority
                    , Settings.Default.LoggingHandlerOrder));
            return container;
        }

        private static void DefineLogger(IConfigurationSourceBuilder builder, IConfigurationSource source)
        {
            builder.ConfigureLogging()
                   .WithOptions
                     .DoNotRevertImpersonation()
                   .LogToCategoryNamed(Settings.Default.LogToCategoryNamed)
                   .WithOptions.SetAsDefaultCategory()
                     .SendTo.RollingFile(Settings.Default.RollingFile)
                     .RollAfterSize(Settings.Default.MaxLogFileSize)
                       .FormatWith(new FormatterBuilder()
                         .TextFormatterNamed(Settings.Default.TextFormatterNamed)
                           .UsingTemplate(@"Timestamp: {timestamp},{newline}Message: {message},{newline}Category: {category},{newline}Severity: {severity},{newline}Title:{title},{newline}ProcessId: {localProcessId},{newline}Process Name: {localProcessName},{newline}Thread Name: {threadName},{newline}Method Name {property(MethodName)},{newline}Method Return Value: {property(ReturnValue)},{newline}Method Call Time: {property(CallTime)},{newline}Method Parameters: {dictionary({key} - {value} )}"))
                           .ToFile(Settings.Default.LogFileFolder);
            var configurationSource = CreateConfigurationSource(builder,source);
            builder.UpdateConfigurationWithReplace(configurationSource);
            Logger.SetLogWriter(new LogWriterFactory(configurationSource).Create());
        }
        private static void DefineExceptionHandling(IConfigurationSourceBuilder builder, IConfigurationSource source)
        {
            builder.ConfigureExceptionHandling()
                .GivenPolicyWithName(Settings.Default.UnityPolicyName)
                .ForExceptionType<Exception>()
                .LogToCategory(Settings.Default.LogToCategoryNamed)
                .UsingExceptionFormatter<TextExceptionFormatter>()
                .WithSeverity(TraceEventType.Critical)
                .ThenNotifyRethrow();
            var configurationSource = CreateConfigurationSource(builder,source);
            var exceptionHandlerFactory = new ExceptionPolicyFactory(configurationSource);
            ExceptionPolicy.SetExceptionManager(exceptionHandlerFactory.CreateManager());
        }

        private static IConfigurationSource CreateConfigurationSource(IConfigurationSourceBuilder builder, IConfigurationSource configurationSource)
        {
            builder.UpdateConfigurationWithReplace(configurationSource);
            return configurationSource;
        }
    }


    public class SmsProvider : ISmsProvider
    {
        public SmsResult SendSms(SmsRequest smsRequest)
        {
            throw new Exception("Hata Oluştu");
            return new SmsResult { Status = 1 };
        }
    }

    public interface ISmsProvider
    {
        SmsResult SendSms(SmsRequest smsRequest);
    }

    public struct SmsResult
    {
        public int Status { get; set; }

        public override string ToString()
        {
            return string.Format("Status: {0}", Status);
        }
    }

    public struct SmsRequest
    {
        public SmsRequest(string phoneNumber, string content)
            : this()
        {
            if (phoneNumber.Length != 10) throw new Exception(" Telefon numarası 10 hane olmalıdır..! ");
            if (string.IsNullOrWhiteSpace(content)) throw new Exception(" Mesaj boş olamaz..! ");
            if (content.Length > 360) throw new Exception(" Mesaj 360 karakterden büyük olamaz ");
            PhoneNumber = phoneNumber;
            Content = content;
        }

        public string PhoneNumber { get; set; }

        public string Content { get; set; }

        public override string ToString()
        {
            return string.Format("PhoneNumber: {0}, Content: {1}", PhoneNumber, Content);
        }
    }
}
