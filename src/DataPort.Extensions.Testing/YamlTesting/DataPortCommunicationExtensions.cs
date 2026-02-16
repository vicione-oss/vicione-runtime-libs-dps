using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ManagedEngine.Communication;

namespace ViciOne.Suite.DataPort.Extensions.Testing.YamlTesting;

/// <summary>
/// Extension methods for DataPortCommunication to support YAML configuration consistency tests.
/// </summary>
public static class DataPortCommunicationExtensions
{
    /// <summary>
    /// Get all public property names from a Communication class.
    /// Excludes inherited properties from DataPortCommunication base.
    /// </summary>
    /// <typeparam name="T">Type that extends DataPortCommunication</typeparam>
    public static List<string> GetCommunicationPropertyNames<T>() where T : DataPortCommunication
    {
        var communicationType = typeof(T);

        var baseProperties = typeof(DataPortCommunication)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(prop => prop.Name).ToList();

        return
        [
            .. communicationType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(prop => prop.Name)
                .Except(baseProperties)
        ];
    }

    /// <summary>
    /// Get the Communication ID from the CommunicationAttribute on a DataPortCommunication type.
    /// </summary>
    /// <typeparam name="T">Type that extends DataPortCommunication</typeparam>
    /// <returns>Communication ID string</returns>
    /// <exception cref="InvalidOperationException">Thrown when CommunicationAttribute is not present</exception>
    public static string GetCommunicationId<T>() where T : DataPortCommunication
    {
        var communicationType = typeof(T);
        var attribute = communicationType.GetCustomAttribute<CommunicationAttribute>() ??
                        throw new InvalidOperationException(
                            $"Type {communicationType.Name} does not have a CommunicationAttribute");
        return attribute.Id;
    }
}
