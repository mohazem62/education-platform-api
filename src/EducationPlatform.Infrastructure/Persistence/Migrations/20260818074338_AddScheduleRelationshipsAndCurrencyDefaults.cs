using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleRelationshipsAndCurrencyDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherStudentAssignments_TeacherId_StudentId_SubjectId",
                table: "TeacherStudentAssignments");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "TeacherStudentAssignments",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EGP");

            migrationBuilder.AddColumn<decimal>(
                name: "SessionPrice",
                table: "TeacherStudentAssignments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "Teachers",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EGP");

            migrationBuilder.AddColumn<string>(
                name: "ZoomMeetingUrl",
                table: "Teachers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentPriceCurrencySnapshot",
                table: "Sessions",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EGP");

            migrationBuilder.AddColumn<decimal>(
                name: "StudentPriceSnapshot",
                table: "Sessions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TeacherRateCurrencySnapshot",
                table: "Sessions",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EGP");

            migrationBuilder.CreateTable(
                name: "TeacherGradeRates",
                columns: table => new
                {
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherGradeRates", x => new { x.TeacherId, x.GradeLevelId });
                    table.ForeignKey(
                        name: "FK_TeacherGradeRates_GradeLevels_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherGradeRates_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeeklySchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ZoomUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklySchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklySchedules_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklySchedules_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklySchedules_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherStudentAssignments_TeacherId_StudentId_SubjectId",
                table: "TeacherStudentAssignments",
                columns: new[] { "TeacherId", "StudentId", "SubjectId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherGradeRates_GradeLevelId",
                table: "TeacherGradeRates",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklySchedules_StudentId_TeacherId_SubjectId_DayOfWeek_StartTime",
                table: "WeeklySchedules",
                columns: new[] { "StudentId", "TeacherId", "SubjectId", "DayOfWeek", "StartTime" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklySchedules_SubjectId",
                table: "WeeklySchedules",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklySchedules_TeacherId",
                table: "WeeklySchedules",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherGradeRates");

            migrationBuilder.DropTable(
                name: "WeeklySchedules");

            migrationBuilder.DropIndex(
                name: "IX_TeacherStudentAssignments_TeacherId_StudentId_SubjectId",
                table: "TeacherStudentAssignments");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "TeacherStudentAssignments");

            migrationBuilder.DropColumn(
                name: "SessionPrice",
                table: "TeacherStudentAssignments");

            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "ZoomMeetingUrl",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "StudentPriceCurrencySnapshot",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "StudentPriceSnapshot",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "TeacherRateCurrencySnapshot",
                table: "Sessions");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherStudentAssignments_TeacherId_StudentId_SubjectId",
                table: "TeacherStudentAssignments",
                columns: new[] { "TeacherId", "StudentId", "SubjectId" },
                unique: true);
        }
    }
}
