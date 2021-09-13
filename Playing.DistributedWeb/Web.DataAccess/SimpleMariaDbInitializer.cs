using Dapper;
using System;
using System.Text;
using System.Threading.Tasks;
using Web.MessagingModels;

namespace Web.DataAccess
{
	public sealed class SimpleMariaDbInitializer
	{
		#region Implementation details
		private static SimpleMariaDbInitializer _instance;

		private const string SystemDb = "sys";

		static SimpleMariaDbInitializer() {
			_instance = new SimpleMariaDbInitializer();
		}

		private SimpleMariaDbInitializer()
		{

		}
		#endregion

		public static SimpleMariaDbInitializer Instance => _instance;
		
		public async Task InitializeDb(string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))			
				throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or empty.", nameof(connectionString));
			
			var connectionStringBuilder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString);

			var targetDb = connectionStringBuilder.Database;

			connectionStringBuilder.Database = SystemDb;
			
			var checkingConString = connectionStringBuilder.ConnectionString;

			using (var connection = new MySqlConnector.MySqlConnection(checkingConString))
			{				
				var strBuilder = new StringBuilder();
				strBuilder.AppendLine($"create database if not exists {targetDb};");
				strBuilder.AppendLine($"use {targetDb};");				

				//todo later: try to pass params via dapper, does not work for some reason
				// sql injection is running around :)
				strBuilder.AppendLine(@$"create table if not exists `{SampleMessage.TableName}` (
										`{nameof(SampleMessage.Id)}` int(11) NOT NULL AUTO_INCREMENT,
										`{nameof(SampleMessage.SessionId)}` int(11) NOT NULL,
										`{nameof(SampleMessage.WithinSessionMessageId)}` int(11) NOT NULL,
										`{nameof(SampleMessage.NodeOne_Timestamp)}` timestamp NULL DEFAULT NULL,
										`{nameof(SampleMessage.NodeTwo_Timestamp)}` timestamp NULL DEFAULT NULL,
										`{nameof(SampleMessage.NodeThree_Timestamp)}` timestamp NULL DEFAULT NULL,
										`{nameof(SampleMessage.End_Timestamp)}` timestamp NULL DEFAULT NULL,
										PRIMARY KEY(`{nameof(SampleMessage.Id)}`,`{nameof(SampleMessage.SessionId)}`)
)										ENGINE = InnoDB DEFAULT CHARSET = utf8mb3; ");

				var sql = strBuilder.ToString();					
				await connection.ExecuteAsync(sql);		
			}				
		}
	}
}
