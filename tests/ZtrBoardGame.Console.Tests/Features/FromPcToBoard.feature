Feature: From Pc To Board Connection

Scenario: A healthy board announces its presence and the PC acknowledges it
    Given a board is configured to connect to a running PC server
    And the PC server is running
    When the board sends a "hello" request to the PC from it's IP
    And the PC receives the request
    Then the PC's console log should contain message like "Received hello from board"

Scenario: The PC server fails to acknowledge a board
    Given the PC server is running
    And a board becomes unreachable after sending its request
    When the PC receives a "hello" request from Board
    And the PC attempts to send a "hello" request back
    Then the PC's console should contain an message like "Cannot connect to the board"

Scenario: The PC server successfully acknowledges board
    Given Board is running
    When the PC receives a "hello" request from Board
	And the PC sends a "hello" request to the Board
    Then the Board's console should contain messages like "Received hello from PC"