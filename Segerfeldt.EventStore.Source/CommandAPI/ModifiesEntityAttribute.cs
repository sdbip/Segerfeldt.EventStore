using JetBrains.Annotations;

using Microsoft.OpenApi.Models;

using System;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    [AttributeUsage(AttributeTargets.Class)]
    [PublicAPI, MeansImplicitUse]
    public class ModifiesEntityAttribute : Attribute
    {
        public const string DefaultEntityId = "entityId";

        public string Entity { get; }
        public OperationType Method { get; set; }
        public string? EntityId { get; init; }
        public string? Property { get; init; }
        public string? PropertyId { get; init; }

        internal string EntityIdOrDefault => EntityId ?? DefaultEntityId;
        internal bool HasEntityIdParameter => Method == OperationType.Delete || Property is not null;
        public bool HasPropertyIdParameter => PropertyId is not null;

        internal string Pattern =>
            Property is not null ? SpecificPropertyPattern :
            EntityId is not null ? SpecificEntityPattern :
            BaseEntityPattern;

        private string SpecificPropertyPattern => $"{SpecificEntityPattern}/{Property}{(PropertyId is null ? "" : $"/{{{PropertyId}}}")}";
        private string SpecificEntityPattern => $"{BaseEntityPattern}/{{{EntityIdOrDefault}}}";

        private string BaseEntityPattern => $"/{Entity.ToLowerInvariant()}";

        public ModifiesEntityAttribute(string entity) : this(entity, OperationType.Post) { }
        protected ModifiesEntityAttribute(string entity, OperationType method)
        {
            Entity = entity;
            Method = method;
        }
    }

    public sealed class DeletesEntityAttribute : ModifiesEntityAttribute
    {
        public DeletesEntityAttribute(string entity) : base(entity, OperationType.Delete)
        {
            EntityId = DefaultEntityId;
        }
    }

    public sealed class AddsEntityAttribute : ModifiesEntityAttribute
    {
        public AddsEntityAttribute(string entity) : base(entity, OperationType.Post) { }
    }
}
