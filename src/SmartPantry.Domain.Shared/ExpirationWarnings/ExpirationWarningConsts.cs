using System;

namespace SmartPantry.ExpirationWarnings;

public static class ExpirationWarningConsts
{
    public const int MaxLogicalKeyLength = 100;

    /// <summary>Umbral de aviso: se avisa cuando faltan 3 días o menos (día límite incluido).</summary>
    public const int DefaultThresholdDays = 3;

    public static string BuildLogicalKey(Guid pantryItemId, ExpirationWarningType type)
        => $"{pantryItemId:N}:{type}";
}