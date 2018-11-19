using System.Reflection;

namespace InfinityBot
{
    public class Information
    {
        /// <summary>
        /// GitHub link to the project.
        /// </summary>
        public static string GitHub => "https://github.com/InfinityGhost/InfinityBot";

        /// <summary>
        /// Version of the program.
        /// </summary>
        public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// Information regarding Discord.
        /// </summary>
        public class Discord
        {
            /// <summary>
            /// Discord tag of creator.
            /// </summary>
            public static string Tag => "InfinityGhost#7843";

            /// <summary>
            /// Development Discord link.
            /// </summary>
            public static string DevLink => "https://discord.gg/aQSZ2WC";
        }
    }
}
