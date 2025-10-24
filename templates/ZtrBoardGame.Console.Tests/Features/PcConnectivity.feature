Feature: PC and Board Discovery

Scenario: A healthy board announces its presence and the PC acknowledges it
    Given a board is configured with the PC server address
    And the PC server is running
    When the board sends a "hello" request to the PC from IP "192.168.1.101"
    And the PC receives the request
    Then the PC's console log should contain an INFO message like "Received hello from board at 192.168.1.101"
    And the PC should immediately send a "hello" request back to "http://192.168.1.101/api/hello"
    And the PC's console log should contain an INFO message like "Responded with hello to board at 192.168.1.101"
