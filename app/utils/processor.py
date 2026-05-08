import subprocess
import os
import re

def process_file(filename):
    """
    Processes a file using a system command.
    Safely handles filename to prevent command injection.
    """
    # Basic validation: ensure filename doesn't contain path separators
    # and is not empty. Also check for common shell metacharacters.
    if not filename or \
       os.path.sep in filename or \
       (os.path.altsep and os.path.altsep in filename) or \
       re.search(r'[;&|`$()<>\"]', filename):
        raise ValueError("Invalid filename provided.")

    try:
        # Fix: Pass command and arguments as a list to prevent shell interpretation
        # and remove shell=True.
        result = subprocess.run(["cat", filename], capture_output=True, text=True, check=True)
        return result.stdout
    except subprocess.CalledProcessError as e:
        return f"Error: {e.stderr}"
    except FileNotFoundError:
        return f"Error: The file '{filename}' was not found."