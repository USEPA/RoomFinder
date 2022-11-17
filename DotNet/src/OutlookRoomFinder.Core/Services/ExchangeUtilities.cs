using Microsoft.Graph;
using OutlookRoomFinder.Core.Exceptions;
using OutlookRoomFinder.Core.Models.Outlook;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OutlookRoomFinder.Core.Services
{
    public static class ExchangeUtilities
    {
        /// <summary>
        /// Regular expression for legal domain names.
        /// </summary>
        internal const string DomainRegex = "^[-a-zA-Z0-9_.]+$";


        #region EmailAddress parsing

        /// <summary>
        /// Gets the domain name from an email address.
        /// </summary>
        /// <param name="emailAddress">The email address.</param>
        /// <returns>Domain name.</returns>
        internal static string DomainFromEmailAddress(string emailAddress)
        {
            string[] emailAddressParts = emailAddress.Split('@');

            if (emailAddressParts.Length != 2 || string.IsNullOrEmpty(emailAddressParts[1]))
            {
                throw new FormatException(ResourceStrings.InvalidEmailAddress);
            }

            return emailAddressParts[1];
        }

        #endregion


        #region Method parameters validation routines

        /// <summary>
        /// Validates parameter (and allows null value).
        /// </summary>
        /// <param name="param">The param.</param>
        /// <param name="paramName">Name of the param.</param>
        internal static void ValidateParamAllowNull(object param, string paramName)
        {
            if (param is ISelfValidate selfValidate)
            {
                try
                {
                    selfValidate.Validate();
                }
                catch (ServiceValidationException e)
                {
                    throw new ArgumentException(
                        ResourceStrings.ValidationFailed,
                        paramName,
                        e);
                }
            }

            if (param is Event ewsObject && string.IsNullOrEmpty(ewsObject.ICalUId))
            {
                throw new ArgumentException(ResourceStrings.ObjectDoesNotHaveId, paramName);
            }
        }

        /// <summary>
        /// Validates parameter (null value not allowed).
        /// </summary>
        /// <param name="param">The param.</param>
        /// <param name="paramName">Name of the param.</param>
        internal static void ValidateParam(object param, string paramName)
        {
            bool isValid = param is string strParam ? !string.IsNullOrEmpty(strParam) : param != null;
            if (!isValid)
            {
                throw new ArgumentNullException(paramName);
            }

            ValidateParamAllowNull(param, paramName);
        }

        /// <summary>
        /// Validates parameter collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="paramName">Name of the param.</param>
        internal static void ValidateParamCollection(IEnumerable collection, string paramName)
        {
            ValidateParam(collection, paramName);

            int count = 0;

            foreach (object obj in collection)
            {
                try
                {
                    ValidateParam(obj, string.Format("collection[{0}]", count));
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(
                        string.Format("The element at position {0} is invalid", count),
                        paramName,
                        e);
                }

                count++;
            }

            if (count == 0)
            {
                throw new ArgumentException(ResourceStrings.CollectionIsEmpty, paramName);
            }
        }

        /// <summary>
        /// Validates string parameter to be non-empty string (null value allowed).
        /// </summary>
        /// <param name="param">The string parameter.</param>
        /// <param name="paramName">Name of the parameter.</param>
        internal static void ValidateNonBlankStringParamAllowNull(string param, string paramName)
        {
            // Non-empty string has at least one character which is *not* a whitespace character
            if (param != null && param.Length == param.CountMatchingChars((c) => Char.IsWhiteSpace(c)))
            {
                throw new ArgumentException(ResourceStrings.ArgumentIsBlankString, paramName);
            }
        }

        /// <summary>
        /// Validates string parameter to be non-empty string (null value not allowed).
        /// </summary>
        /// <param name="param">The string parameter.</param>
        /// <param name="paramName">Name of the parameter.</param>
        internal static void ValidateNonBlankStringParam(string param, string paramName)
        {
            if (param == null)
            {
                throw new ArgumentNullException(paramName);
            }

            ValidateNonBlankStringParamAllowNull(param, paramName);
        }

        /// <summary>
        /// Validates domain name (null value allowed)
        /// </summary>
        /// <param name="domainName">Domain name.</param>
        /// <param name="paramName">Parameter name.</param>
        internal static void ValidateDomainNameAllowNull(string domainName, string paramName)
        {
            if (domainName != null)
            {
                Regex regex = new Regex(DomainRegex);

                if (!regex.IsMatch(domainName))
                {
                    throw new ArgumentException(string.Format(ResourceStrings.InvalidDomainName, domainName), paramName);
                }
            }
        }


        /// <summary>
        /// Asserts that the specified condition if true.
        /// </summary>
        /// <param name="condition">Assertion.</param>
        /// <param name="caller">The caller.</param>
        /// <param name="message">The message to use if assertion fails.</param>
        internal static void Assert(
            bool condition,
            string caller,
            string message)
        {
            Debug.Assert(
                condition,
                string.Format("[{0}] {1}", caller, message));
        }

        /// <summary>
        /// Gets the schema name for enum member.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="enumName">The enum name.</param>
        /// <returns>The name for the enum used in the protocol, or null if it is the same as the enum's ToString().</returns>
        internal static string GetEnumSchemaName(Type enumType, string enumName)
        {
            MemberInfo[] memberInfo = enumType.GetMember(enumName);
            Assert((memberInfo != null) && (memberInfo.Length > 0),
                    "ExchangeUtilities.GetEnumSchemaName",
                    $"Enum member {enumName} not found in {enumType}");

            object[] attrs = memberInfo.FirstOrDefault().GetCustomAttributes(typeof(EwsEnumAttribute), false);
            return attrs != null && attrs.Length > 0 ? ((EwsEnumAttribute)attrs[0]).SchemaName : null;
        }

        /// <summary>
        /// Builds the schema to enum mapping dictionary.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <returns>The mapping from enum to schema name</returns>
        internal static Dictionary<string, Enum> BuildSchemaToEnumDict(Type enumType)
        {
            Dictionary<string, Enum> dict = new Dictionary<string, Enum>();
            string[] names = Enum.GetNames(enumType);
            foreach (string name in names)
            {
                Enum value = (Enum)Enum.Parse(enumType, name, false);
                string schemaName = ExchangeUtilities.GetEnumSchemaName(enumType, name);

                if (!String.IsNullOrEmpty(schemaName))
                {
                    dict.Add(schemaName, value);
                }
            }
            return dict;
        }

        /// <summary>
        /// Builds the enum to schema mapping dictionary.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <returns>The mapping from enum to schema name</returns>
        internal static Dictionary<Enum, string> BuildEnumToSchemaDict(Type enumType)
        {
            Dictionary<Enum, string> dict = new Dictionary<Enum, string>();
            string[] names = Enum.GetNames(enumType);
            foreach (string name in names)
            {
                Enum value = (Enum)Enum.Parse(enumType, name, false);
                string schemaName = ExchangeUtilities.GetEnumSchemaName(enumType, name);

                if (!String.IsNullOrEmpty(schemaName))
                {
                    dict.Add(value, schemaName);
                }
            }
            return dict;
        }
        #endregion

        #region Extension methods
        /// <summary>
        /// Count characters in string that match a condition.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="charPredicate">Predicate to evaluate for each character in the string.</param>
        /// <returns>Count of characters that match condition expressed by predicate.</returns>
        internal static int CountMatchingChars(this string str, Predicate<char> charPredicate)
        {
            int count = 0;
            foreach (char ch in str)
            {
                if (charPredicate(ch))
                {
                    count++;
                }
            }

            return count;
        }

        #endregion
    }
}
