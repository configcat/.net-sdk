using System;

namespace ConfigCat.Client;

/// <summary>
/// Log event identifier.
/// </summary>
public readonly struct LogEventId : IEquatable<LogEventId>
{
    /// <summary>
    /// Implicitly converts the given <see cref="int"/> value to an <see cref="LogEventId"/>.
    /// </summary>
    /// <param name="id">The <see cref="int"/> value.</param>
    public static implicit operator LogEventId(int id)
    {
        return new LogEventId(id);
    }

    /// <summary>
    /// Checks if two specified <see cref="LogEventId"/> instances have the same value. They are equal if they have the same Id.
    /// </summary>
    public static bool operator ==(LogEventId left, LogEventId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks if two specified <see cref="LogEventId"/> instances have different values.
    /// </summary>
    public static bool operator !=(LogEventId left, LogEventId right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Initializes an instance of the <see cref="LogEventId"/> struct.
    /// </summary>
    /// <param name="id">The numeric identifier for the event.</param>
    public LogEventId(int id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the numeric identifier for this event.
    /// </summary>
    public int Id { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return Id.ToString();
    }

    /// <inheritdoc/>
    public bool Equals(LogEventId other)
    {
        return Id == other.Id;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is LogEventId other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
