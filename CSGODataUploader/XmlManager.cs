#region

using System;
using System.IO;
using System.Xml.Serialization;

#endregion

namespace Hearthstone_Deck_Tracker
{
	public static class XmlManager<T>
	{
		public static T Load(string path)
		{
			T instance;
			using(TextReader reader = new StreamReader(path))
			{
				var xml = new XmlSerializer(typeof(T));
				instance = (T)xml.Deserialize(reader);
			}

			return instance;
		}

		public static T LoadFromString(string xmlString)
		{
			T instance;
			using(TextReader reader = new StringReader(xmlString))
			{
				var xml = new XmlSerializer(typeof(T));
				instance = (T)xml.Deserialize(reader);
			}
			return instance;
		}

		public static void Save(string path, object obj)
		{
			var i = 0;
			var deleteBackup = true;
			var backupPath = path.Replace(".xml", "_backup.xml");

			//make sure not to overwrite backups that could not be restored (were not deleted)
			while(File.Exists(backupPath))
				backupPath = path.Replace(".xml", "_backup" + i++ + ".xml");


			//create backup
			if(File.Exists(path))
				File.Copy(path, backupPath);
			try
			{
				//standard serialization
				using(TextWriter writer = new StreamWriter(path))
				{
					var xml = new XmlSerializer(typeof(T));
					xml.Serialize(writer, obj);
				}
				
			}
			catch(Exception e)
			{
				try
				{
					//restore backup
					File.Delete(path);
					if(File.Exists(backupPath))
						File.Move(backupPath, path);
				}
				catch(Exception e2)
				{
					//restoring failed 
					deleteBackup = false;
				}
			}
			finally
			{
				if(deleteBackup && File.Exists(backupPath))
				{
					try
					{
						File.Delete(backupPath);
					}
					catch(Exception)
					{
						//note sure, todo?
					}
				}
			}
		}
	}
}