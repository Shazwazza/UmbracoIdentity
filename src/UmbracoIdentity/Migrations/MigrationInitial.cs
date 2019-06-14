using Umbraco.Core.Migrations;
using static UmbracoIdentity.ExternalLoginStore;

namespace UmbracoIdentity.Migrations
{
	public class MigrationInitial : MigrationBase
	{
		public MigrationInitial(IMigrationContext context)
			: base(context)
		{ }

		public override void Migrate()
		{
			if (!TableExists(TableName))
				Create.Table<ExternalLoginDto>().Do();
		}
	}
}
