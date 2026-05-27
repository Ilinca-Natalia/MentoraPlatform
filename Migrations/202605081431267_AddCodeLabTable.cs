namespace MentoraPlatform.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCodeLabTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CodeProjects",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false),
                        HtmlCode = c.String(),
                        CssCode = c.String(),
                        JsCode = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                        UserId = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CodeProjects", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.CodeProjects", new[] { "UserId" });
            DropTable("dbo.CodeProjects");
        }
    }
}
