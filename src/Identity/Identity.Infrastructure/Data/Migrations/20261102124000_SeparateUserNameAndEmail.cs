using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Data.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20261102124000_SeparateUserNameAndEmail")]
public partial class SeparateUserNameAndEmail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH generated_user_names AS (
                SELECT
                    "Id",
                    COALESCE(
                        NULLIF(
                            LEFT(
                                REGEXP_REPLACE(
                                    SPLIT_PART(COALESCE("Email", ''), '@', 1),
                                    '[^a-zA-Z0-9._+-]',
                                    '',
                                    'g'
                                ),
                                200
                            ),
                            ''
                        ),
                        'user'
                    ) || '-' || REPLACE("Id"::text, '-', '') AS user_name
                FROM identity."AspNetUsers"
            )
            UPDATE identity."AspNetUsers" AS users
            SET
                "UserName" = generated_user_names.user_name,
                "NormalizedUserName" = UPPER(generated_user_names.user_name)
            FROM generated_user_names
            WHERE users."Id" = generated_user_names."Id";
            """);

        migrationBuilder.DropIndex(
            name: "EmailIndex",
            schema: "identity",
            table: "AspNetUsers");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            schema: "identity",
            table: "AspNetUsers",
            column: "NormalizedEmail",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "EmailIndex",
            schema: "identity",
            table: "AspNetUsers");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            schema: "identity",
            table: "AspNetUsers",
            column: "NormalizedEmail");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        new SnapshotModel().Build(modelBuilder);
    }

    private sealed class SnapshotModel : IdentityDbContextModelSnapshot
    {
        public void Build(ModelBuilder modelBuilder)
        {
            BuildModel(modelBuilder);
        }
    }
}
