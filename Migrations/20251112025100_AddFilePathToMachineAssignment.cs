using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddFilePathToMachineAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TechnicalSheet",
                columns: table => new
                {
                    TBKT_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Power_kVA = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VoltageSpec = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StandardCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Proposer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DrawingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalSheet", x => x.TBKT_ID);
                });

            migrationBuilder.CreateTable(
                name: "MachineAssignment",
                columns: table => new
                {
                    AssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TBKT_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StandardRequirement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdditionalRequest = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Designer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TeamLeader = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineAssignment", x => x.AssignmentID);
                    table.ForeignKey(
                        name: "FK_MachineAssignment_TechnicalSheet_TBKT_ID",
                        column: x => x.TBKT_ID,
                        principalTable: "TechnicalSheet",
                        principalColumn: "TBKT_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalNotification",
                columns: table => new
                {
                    NotificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TBKT_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DesignReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TechnicalStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RoutDrawingCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VoDrawingCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VoLength = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VoWidth = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VoHeight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Accessories = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaterialUsage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TechnicalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Signer_Proposal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Signer_Designer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Signer_Approver = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SignDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalNotification", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_TechnicalNotification_TechnicalSheet_TBKT_ID",
                        column: x => x.TBKT_ID,
                        principalTable: "TechnicalSheet",
                        principalColumn: "TBKT_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentApproval",
                columns: table => new
                {
                    ApprovalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentID = table.Column<int>(type: "int", nullable: false),
                    ApproverRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApproverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentApproval", x => x.ApprovalID);
                    table.ForeignKey(
                        name: "FK_AssignmentApproval_MachineAssignment_AssignmentID",
                        column: x => x.AssignmentID,
                        principalTable: "MachineAssignment",
                        principalColumn: "AssignmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkChange",
                columns: table => new
                {
                    ChangeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentID = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkChange", x => x.ChangeID);
                    table.ForeignKey(
                        name: "FK_WorkChange_MachineAssignment_AssignmentID",
                        column: x => x.AssignmentID,
                        principalTable: "MachineAssignment",
                        principalColumn: "AssignmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkItem",
                columns: table => new
                {
                    WorkItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentID = table.Column<int>(type: "int", nullable: false),
                    WorkType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PersonName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedFinish = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualFinish = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersonConfirmation = table.Column<bool>(type: "bit", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItem", x => x.WorkItemID);
                    table.ForeignKey(
                        name: "FK_WorkItem_MachineAssignment_AssignmentID",
                        column: x => x.AssignmentID,
                        principalTable: "MachineAssignment",
                        principalColumn: "AssignmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentApproval_AssignmentID",
                table: "AssignmentApproval",
                column: "AssignmentID");

            migrationBuilder.CreateIndex(
                name: "IX_MachineAssignment_MachineName",
                table: "MachineAssignment",
                column: "MachineName");

            migrationBuilder.CreateIndex(
                name: "IX_MachineAssignment_TBKT_ID",
                table: "MachineAssignment",
                column: "TBKT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalNotification_TBKT_ID",
                table: "TechnicalNotification",
                column: "TBKT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalSheet_TBKT_ID",
                table: "TechnicalSheet",
                column: "TBKT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkChange_AssignmentID",
                table: "WorkChange",
                column: "AssignmentID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItem_AssignmentID",
                table: "WorkItem",
                column: "AssignmentID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentApproval");

            migrationBuilder.DropTable(
                name: "TechnicalNotification");

            migrationBuilder.DropTable(
                name: "WorkChange");

            migrationBuilder.DropTable(
                name: "WorkItem");

            migrationBuilder.DropTable(
                name: "MachineAssignment");

            migrationBuilder.DropTable(
                name: "TechnicalSheet");
        }
    }
}
