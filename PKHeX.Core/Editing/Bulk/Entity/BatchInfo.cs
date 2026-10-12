namespace PKHeX.Core;

/// <summary>
/// Information wrapper used for Batch Editing to apply suggested values.
/// </summary>
/// <param name="Entity"> Entity to be modified. </param>
public sealed record BatchInfo(PKM Entity)
{
    /// <summary>
    /// Legality analysis of the entity.
    /// </summary>
    /// <remarks>
    /// Fork: evaluated on first use, not in the ctor. Bot sets put property edits (e.g. <c>.CurrentLevel=50</c>) ahead of
    /// <c>$suggest</c> instructions, and the suggestions must see the edited entity; an eager analysis made them suggest
    /// Plus moves for the generated level instead of the requested one.
    /// </remarks>
    public LegalityAnalysis Legality => field ??= new LegalityAnalysis(Entity);

    /// <inheritdoc cref="LegalityAnalysis.Valid"/>
    public bool Legal => Legality.Valid;
}
