namespace LocalSpeechRecognitionMaster.DataModels
{
    public static class Actions
    {
        private readonly static HashSet<string> actions = new();

        //Add action
        public static void AddAction(string action)
        {
            actions.Add(action);
        }

        public static bool ActionExists(string action)
        {
            return actions.Contains(action);
        }
    }
}
