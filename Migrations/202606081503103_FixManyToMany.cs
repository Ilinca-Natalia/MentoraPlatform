namespace MentoraPlatform.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixManyToMany : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.AspNetUsers", "Course_Id", "dbo.Courses");
            DropIndex("dbo.AspNetUsers", new[] { "Course_Id" });
            CreateTable(
                "dbo.CourseStudents",
                c => new
                    {
                        CourseId = c.Int(nullable: false),
                        StudentId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.CourseId, t.StudentId })
                .ForeignKey("dbo.Courses", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.StudentId, cascadeDelete: true)
                .Index(t => t.CourseId)
                .Index(t => t.StudentId);
            
            DropColumn("dbo.AspNetUsers", "Course_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AspNetUsers", "Course_Id", c => c.Int());
            DropForeignKey("dbo.CourseStudents", "StudentId", "dbo.AspNetUsers");
            DropForeignKey("dbo.CourseStudents", "CourseId", "dbo.Courses");
            DropIndex("dbo.CourseStudents", new[] { "StudentId" });
            DropIndex("dbo.CourseStudents", new[] { "CourseId" });
            DropTable("dbo.CourseStudents");
            CreateIndex("dbo.AspNetUsers", "Course_Id");
            AddForeignKey("dbo.AspNetUsers", "Course_Id", "dbo.Courses", "Id");
        }
    }
}
