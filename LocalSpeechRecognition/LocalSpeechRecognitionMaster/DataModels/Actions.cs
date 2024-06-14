namespace LocalSpeechRecognitionMaster.DataModels
{
    public static class Actions
    {   // Klasse wird eigentlich nicht mehr benötigt. Wird aber weiterhin implementiert für zukünftige Entwicklungen
        private readonly static HashSet<string> actions = new();

        //Add action
        public static void AddAction(string action)
        {
            actions.Add(action);
        }
    }
}
