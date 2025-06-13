using System.Linq;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;

#if BENCHMARK_OLD
namespace ConfigCat.Client.Benchmarks.Old;
#else
namespace ConfigCat.Client.Benchmarks.New;
#endif

internal class EvaluationServices
{
    public EvaluationServices(bool logInfo)
    {
        Logger = new LoggerWrapper(new NullLogger { LogLevel = logInfo ? LogLevel.Info : LogLevel.Warning });
        Evaluator = new RolloutEvaluator(Logger);
    }

    public LoggerWrapper Logger { get; }
    public RolloutEvaluator Evaluator { get; }
}

public static partial class BenchmarkHelper
{
    public static object CreateEvaluationServices(bool logInfo) => new EvaluationServices(logInfo);

    public static object?[][] GetMatrixTests<TDescriptor>()
        where TDescriptor : IMatrixTestDescriptor, new()
    {
        return MatrixTestRunnerBase<TDescriptor>.GetTests().ToArray();
    }

    public static bool RunMatrixTest<TDescriptor>(this MatrixTestRunnerBase<TDescriptor> runner, object evaluationServices, string settingKey, string expectedReturnValue, User? user = null)
        where TDescriptor : IMatrixTestDescriptor, new()
    {
        var services = (EvaluationServices)evaluationServices;
        return runner.RunTest(services.Evaluator, services.Logger, settingKey, expectedReturnValue, user);
    }

    public static int RunAllMatrixTests<TDescriptor>(this MatrixTestRunnerBase<TDescriptor> runner, object evaluationServices, object?[][] tests)
        where TDescriptor : IMatrixTestDescriptor, new()
    {
        var services = (EvaluationServices)evaluationServices;
        return runner.RunAllTests(services.Evaluator, services.Logger, tests);
    }

    public static EvaluationDetails<T> Evaluate<T>(object evaluationServices, string key, T defaultValue, User? user = null)
    {
        var services = (EvaluationServices)evaluationServices;
        return services.Evaluator.Evaluate(Config.GetSettings(), key, defaultValue, user, remoteConfig: null, services.Logger);
    }

    public static EvaluationDetails[] EvaluateAll(object evaluationServices, User? user = null)
    {
        var services = (EvaluationServices)evaluationServices;
        return services.Evaluator.EvaluateAll(Config.GetSettings(), user, remoteConfig: null, services.Logger, "empty array", out _);
    }
}
