namespace ReSpekter
{
    using Mono.Cecil;

    /// <summary>
    /// Manages all <see cref="ModuleDefinition"/>s for this context.
    /// </summary>
    public class ModuleManager
    {
        /// <summary>
        /// The context for this manager.
        /// </summary>
        private readonly Context _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleManager"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public ModuleManager(Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Finds the specified reference in the context.
        /// </summary>
        /// <param name="original">The original reference.</param>
        /// <returns>The corresponding reference in the context.</returns>
        public TypeReference FindReference(TypeReference original)
        {
            // TODO: lookup reference or create new reference for filtered type
            //_context.FilterHost.Process();

            return original;
        }
    }
}