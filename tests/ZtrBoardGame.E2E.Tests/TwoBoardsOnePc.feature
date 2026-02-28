@MyCategory
Feature: Two Boards one PC
	As a user
	I want to run a PC server and two boards in Docker
	To ensure they can all connect to each other

Scenario: Two boards connect to one PC server
	Given a running Docker environment
	When I send a GET request to "api/boards"
	Then the response should contain two boards