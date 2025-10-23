Feature: Board and PC Discovery

Scenario: Board starts with a valid server address configuration
    Given the board's configuration specifies the PC server address as "http://192.168.0.200:8080"
    When the board application starts
    Then the application should run without startup errors
    And the board should begin its announcement cycle to "http://192.168.0.200:8080/api/hello"

Scenario: Board starts without a server address configuration
    Given the board's configuration does not specify the PC server address
    When the board application starts
    Then the application should fail to start
    And a log entry with a clear error "PC server address is not configured" should be created
