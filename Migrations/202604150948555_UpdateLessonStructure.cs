namespace MentoraPlatform.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateLessonStructure : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LessonAttachments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FileName = c.String(),
                        FilePath = c.String(),
                        FileType = c.String(),
                        LessonId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Lessons", t => t.LessonId, cascadeDelete: true)
                .Index(t => t.LessonId);
            
            CreateTable(
                "dbo.UserLessonProgresses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(maxLength: 128),
                        LessonId = c.Int(nullable: false),
                        CompletedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Lessons", t => t.LessonId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.LessonId);
            
            AddColumn("dbo.Lessons", "VideoUrl", c => c.String());
            AlterColumn("dbo.Lessons", "Content", c => c.String());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserLessonProgresses", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.UserLessonProgresses", "LessonId", "dbo.Lessons");
            DropForeignKey("dbo.LessonAttachments", "LessonId", "dbo.Lessons");
            DropIndex("dbo.UserLessonProgresses", new[] { "LessonId" });
            DropIndex("dbo.UserLessonProgresses", new[] { "UserId" });
            DropIndex("dbo.LessonAttachments", new[] { "LessonId" });
            AlterColumn("dbo.Lessons", "Content", c => c.String(nullable: false));
            DropColumn("dbo.Lessons", "VideoUrl");
            DropTable("dbo.UserLessonProgresses");
            DropTable("dbo.LessonAttachments");
        }
    }
}
