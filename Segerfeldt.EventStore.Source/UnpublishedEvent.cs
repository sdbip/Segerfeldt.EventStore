namespace Segerfeldt.EventStore.Source;

/// <summary>An event that hasn't been published yet</summary>
/// The event should include all the details needed to unambiguously
/// define the new state of an entity (given an unambiguous prior state).
/// The event should represent a small, but meaningful change so that it
/// can be reused to model the effects of multiple commands/activities.
/// <example>
/// A Student entity might enroll in a course:
/// <code>
/// {
///   Name = "Enrolled";
///   Details = {CourseId = "CS193p"}
/// }
/// </code>
/// </example>
public sealed class UnpublishedEvent
{
    /// <summary>In what way the state of entity will change when this event is published.</summary>
    /// Should represent a small, but meaningful change so that it can be reused to model
    /// the effects of multiple commands/activities.
    /// <example><c>"Enrolled"</c></example>
    public string Name { get; }

    /// <summary>The details of the change</summary>
    /// <example><c>{"courseId": "CS193p"}</c></example>
    public object Details { get; }

    /// <param name="name">what aspect of the entity is changing</param>
    /// <param name="details">details regarding the change</param>
    /// <example><c>
    /// new (
    ///   name: "Enrolled",
    ///   details: new {CourseId = "CS193p"}
    /// )
    /// </c></example>
    public UnpublishedEvent(string name, object details)
    {
        Name = name;
        Details = details;
    }
}
