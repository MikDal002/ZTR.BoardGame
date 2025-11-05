Feature: Board Connector Service
    As a PC
    I want to connect to the board
    So that I can send commands to it

Scenario: Board is not available
    Given PC is running
    And board is connected to the PC
    When board is not available
    Then PC will try to connect to the board
    And it will fail
