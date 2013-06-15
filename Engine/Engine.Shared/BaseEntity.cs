using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FTW.Engine.Shared
{
    public abstract class BaseEntity
    {
        public BaseEntity()
        {
            IsDeleted = false;
        }

        /// <summary>
        /// Indicates that an entity is scheduled for deletion at the end of the current frame.
        /// It should be treated as if it has already been deleted.
        /// </summary>
        public bool IsDeleted { get; protected set; }

        public abstract bool IsNetworked { get; }
    }
}
