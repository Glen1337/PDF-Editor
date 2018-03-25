using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;

namespace PDFConverter.Model
{
    public static class Config
    {
        public static String ReadSettingFor(String LookupKey)
        {
            String resultValue = String.Empty;
            NameValueCollection appSettings;

            try
            {
                appSettings = ConfigurationManager.AppSettings;
            }
            catch { throw new ConfigurationErrorsException("Could not access config app settings."); }

            resultValue = appSettings[LookupKey];
            
            //throw new SettingsPropertyNotFoundException(LookupKey + " not found in config settings");
            
            return resultValue;
        }

        public static void AddSettingFor(String Key, String ValueToAdd)
        {
            Configuration configFile;
            KeyValueConfigurationCollection appSettings;

            try
            {
                configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                appSettings = configFile.AppSettings.Settings;
            }
            catch { throw new ConfigurationErrorsException("Could not access exe or app config file."); }

            if (appSettings[Key] == null) { appSettings.Add(Key, ValueToAdd); }
            else { appSettings[Key].Value = ValueToAdd; }

            try
            {
                configFile.Save(ConfigurationSaveMode.Full);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch { throw new ConfigurationErrorsException("Could not save edited config file"); }
        }
    }
}
