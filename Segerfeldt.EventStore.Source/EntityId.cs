using Segerfeldt.EventStore.Utils;

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>And entity identifier</summary>
    public sealed class EntityId : ValueObject<EntityId>
    {
        private readonly string value;

        /// <summary>Initialize an identifier</summary>
        /// <param name="value">The string value that uniquely identifies the identity (and its events)</param>
        public EntityId(string value)
        {
            this.value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents() => ImmutableArray.Create(value);

        public override string ToString() => value;
    }
}
