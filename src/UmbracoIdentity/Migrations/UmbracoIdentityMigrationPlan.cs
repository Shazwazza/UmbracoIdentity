using Umbraco.Core.Migrations;

namespace UmbracoIdentity.Migrations
{
	public class UmbracoIdentityMigrationPlan : MigrationPlan
	{
		public UmbracoIdentityMigrationPlan()
			: base("UmbracoIdentity")
		{
			From(string.Empty)
				.To<MigrationInitial>("first-migration");
		}
	}
}
