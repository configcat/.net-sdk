using System;

namespace ConfigCat.Extensions.Hosting.Configuration;

/// <summary>
/// Options for configuring the initialization behavior of the ConfigCat SDK services.
/// </summary>
public sealed class ConfigCatInitializerOptions
{
    /// <summary>
    /// Gets or sets the initialization mode.
    /// </summary>
    public ConfigCatInitMode InitMode { get; set; }

    // NOTE: ConfigCatInitializerOptions is not configuration binding-friendly (especially problematic in the case of
    // source generated configuration binding), but we can work this around using a wrapper class.
    internal sealed class BindingWrapper(ConfigCatInitializerOptions options)
    {
        public BindingWrapper()
            : this(new ConfigCatInitializerOptions()) { }

        public InitModeOptions? Init
        {
            get => null; // getter is necessary for the source generated configuration binder, but no need to implement it
            set
            {
                if (value is not null)
                {
                    options.InitMode = value.Mode switch
                    {
                        InitModeEnum.DoNotWaitForClientReady => new ConfigCatInitMode.DoNotWaitForClientReady(),
                        InitModeEnum.WaitForClientReady => new ConfigCatInitMode.WaitForClientReady(value.ThrowOnFailure ?? false),
                        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
                    };
                }
            }
        }
    }

    internal enum InitModeEnum
    {
        DoNotWaitForClientReady,
        WaitForClientReady,
    }

    internal sealed class InitModeOptions
    {
        public InitModeEnum Mode { get; set; }

        public bool? ThrowOnFailure { get; set; }
    }
}
