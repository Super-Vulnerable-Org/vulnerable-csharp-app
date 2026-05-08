import pytest
from app.utils.processor import process_file
import os

# Create a temporary file for testing valid input
@pytest.fixture(scope="module")
def setup_test_file():
    file_content = "This is a test file content."
    with open("test_file.txt", "w") as f:
        f.write(file_content)
    yield "test_file.txt"
    os.remove("test_file.txt")

def test_process_file_command_injection_prevented():
    # Attempt a command injection
    malicious_filename = "nonexistent.txt; echo pwned"
    with pytest.raises(ValueError, match="Invalid filename provided."):
        process_file(malicious_filename)

def test_process_file_valid_input(setup_test_file):
    filename = setup_test_file
    expected_content = "This is a test file content."
    result = process_file(filename)
    assert result.strip() == expected_content

def test_process_file_path_traversal_prevented():
    # Attempt a path traversal
    malicious_filename = "../../../etc/passwd"
    with pytest.raises(ValueError, match="Invalid filename provided."):
        process_file(malicious_filename)

def test_process_file_empty_filename():
    with pytest.raises(ValueError, match="Invalid filename provided."):
        process_file("")