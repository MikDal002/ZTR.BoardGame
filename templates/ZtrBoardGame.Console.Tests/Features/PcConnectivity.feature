Feature: PC and Board Discovery

Scenario: A healthy board announces its presence and the PC acknowledges it
    Given a board is configured with the PC server address
    And the PC server is running
    When the board sends a "hello" request to the PC from it's IP
    And the PC receives the request
    Then the PC's console log should contain message like "Received hello from board"
