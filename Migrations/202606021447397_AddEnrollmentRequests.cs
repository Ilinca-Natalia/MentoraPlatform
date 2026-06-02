namespace MentoraPlatform.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEnrollmentRequests : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EnrollmentRequests",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StudentId = c.String(maxLength: 128),
                        CourseId = c.Int(nullable: false),
                        RequestDate = c.DateTime(nullable: false),
                        IsPending = c.Boolean(nullable: false),
                        IsApproved = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.StudentId)
                .Index(t => t.StudentId)
                .Index(t => t.CourseId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.EnrollmentRequests", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.EnrollmentRequests", "CourseId", "dbo.Courses");
            DropIndex("dbo.EnrollmentRequests", new[] { "CourseId" });
            DropIndex("dbo.EnrollmentRequests", new[] { "StudentId" });
            DropTable("dbo.EnrollmentRequests");
        }
    }
}
