using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationProjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add nullable first so existing notifications are not stamped with an empty
            // project id, then backfill each row from its owning task before enforcing
            // NOT NULL. Every notification references a task, and a task always has a
            // project, so the backfill fully populates real data (a task is required by
            // the domain and the FK). The default only guards rows a race could insert
            // between these steps.
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE notifications AS n
                SET "ProjectId" = t."ProjectId"
                FROM task_items AS t
                WHERE n."TaskItemId" = t."Id";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "notifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_ProjectId",
                table: "notifications",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_ProjectId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "notifications");
        }
    }
}
