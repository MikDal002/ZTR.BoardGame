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
    And an error message "PC server address is not configured" should be displayed in the console

Scenario: A board fails to connect to the PC server
    Given a board is configured with the PC server address
    And the PC server is not reachable
    When the board attempts to send a "hello" request
    Then the board's local console log should contain an ERROR message with a reason, such as "Connection refused" or "Host not found"
