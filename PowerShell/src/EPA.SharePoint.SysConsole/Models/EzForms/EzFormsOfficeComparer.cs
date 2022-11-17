using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    /// <summary>
    /// Object comparer for processing <see cref="EzFormsOffice"/>
    /// </summary>
    public class EzFormsOfficeComparer : IEqualityComparer<EzFormsOffice>
    {
        /// <summary>
        /// Compare the label and AA-Ship
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(EzFormsOffice x, EzFormsOffice y)
        {

            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            //Check whether the products' properties are equal.
            return x.label == y.label && x.aaship == y.aaship;
        }

        /// <summary>
        /// If Equals() returns true for a pair of objects then GetHashCode() must return the same value for these objects.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int GetHashCode(EzFormsOffice item)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(item, null)) return 0;

            //Get hash code for the Name field if it is not null.
            int hashName = item.label == null ? 0 : item.label.GetHashCode();

            //Get hash code for the Code field.
            int hashCode = item.aaship.GetHashCode();

            //Calculate the hash code for the product.
            return hashName ^ hashCode;
        }

    }
}
