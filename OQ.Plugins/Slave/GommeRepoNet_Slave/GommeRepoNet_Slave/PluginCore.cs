using GommeRepoNet_Slave.Tasks;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GommeRepoNet_Slave
{
    [Plugin(1, "GommeReportNet_Slave", "These are the Slaves (Actual Report Bots) for the GommeReportNet.", "adiber.at")]
    public class ExamplePlugin : IStartPlugin
    {

        // Must be overriden by every plugin.
        public override void OnLoad(int version, int subversion, int buildversion)
        {
            // Should be used to define all the settings.
            this.Setting.Add(new StringSetting("Master", "The Master-Slave of the GommeReportNet", ""));
        }

        public override PluginResponse OnEnable(IBotSettings botSettings)
        {
            // Called once the plugin is ticked in the plugin tab.
            if (string.IsNullOrWhiteSpace(Setting.At(0).Get<string>())) return new PluginResponse(false, "The Master-Slave must be set!");
            return new PluginResponse(true);
        }

        public override void OnDisable()
        {
            // Called once the plugin is unticked.
            // (Note: does not get called if the plugin is stopped from different sources, such as macros)
            Console.WriteLine("Plugin disabled");
        }

        public override void OnStart()
        {
            // This should be used to register the tasks for the bot, see below.
            // (Note: called after 'OnEnable')
            RegisterTask(new Report(
                Setting.At(0).Get<string>()
                ));
        }

        public override void OnStop()
        {
            // Called once the plugin is stopped.
            // (Note: unlike 'OnDisabled' this gets triggered from other sources, not only plugins tab)
        }
    }
}
