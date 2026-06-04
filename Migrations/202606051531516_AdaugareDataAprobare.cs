namespace MentoraPlatform.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AdaugareDataAprobare : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.EnrollmentRequests", "ApprovalDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.EnrollmentRequests", "ApprovalDate");
        }
    }
}
