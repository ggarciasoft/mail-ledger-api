using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoggingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create application_logs table for general logging
            migrationBuilder.Sql(@"
                CREATE TABLE application_logs (
                    id SERIAL PRIMARY KEY,
                    timestamp TIMESTAMP NOT NULL,
                    level VARCHAR(50) NOT NULL,
                    logger VARCHAR(255) NOT NULL,
                    message TEXT,
                    exception TEXT,
                    thread VARCHAR(255),
                    user_id VARCHAR(255),
                    machine_name VARCHAR(255),
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX idx_application_logs_timestamp ON application_logs(timestamp);
                CREATE INDEX idx_application_logs_level ON application_logs(level);
                CREATE INDEX idx_application_logs_user_id ON application_logs(user_id);
            ");

            // Create error_logs table for error-specific logging
            migrationBuilder.Sql(@"
                CREATE TABLE error_logs (
                    id SERIAL PRIMARY KEY,
                    timestamp TIMESTAMP NOT NULL,
                    level VARCHAR(50) NOT NULL,
                    logger VARCHAR(255) NOT NULL,
                    message TEXT,
                    exception TEXT,
                    stack_trace TEXT,
                    thread VARCHAR(255),
                    user_id VARCHAR(255),
                    machine_name VARCHAR(255),
                    request_path VARCHAR(500),
                    request_method VARCHAR(10),
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX idx_error_logs_timestamp ON error_logs(timestamp);
                CREATE INDEX idx_error_logs_level ON error_logs(level);
                CREATE INDEX idx_error_logs_user_id ON error_logs(user_id);
                CREATE INDEX idx_error_logs_request_path ON error_logs(request_path);
            ");

            // Create audit_logs_table for audit trail logging
            migrationBuilder.Sql(@"
                CREATE TABLE audit_logs_table (
                    id SERIAL PRIMARY KEY,
                    timestamp TIMESTAMP NOT NULL,
                    user_id VARCHAR(255),
                    action VARCHAR(100),
                    entity_type VARCHAR(100),
                    entity_id VARCHAR(255),
                    old_values TEXT,
                    new_values TEXT,
                    ip_address VARCHAR(50),
                    user_agent VARCHAR(500),
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX idx_audit_logs_timestamp ON audit_logs_table(timestamp);
                CREATE INDEX idx_audit_logs_user_id ON audit_logs_table(user_id);
                CREATE INDEX idx_audit_logs_action ON audit_logs_table(action);
                CREATE INDEX idx_audit_logs_entity_type ON audit_logs_table(entity_type);
                CREATE INDEX idx_audit_logs_entity_id ON audit_logs_table(entity_id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS application_logs;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS error_logs;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS audit_logs_table;");
        }
    }
}
