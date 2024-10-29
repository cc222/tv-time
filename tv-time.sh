#!/bin/bash

# Ensure the path to C# binary is provided
if [ -z "$GET_TVSHOW_TOTAL_LENGTH_BIN" ]; then
  echo "Error: GET_TVSHOW_TOTAL_LENGTH_BIN environment variable is not set."
  exit 1
fi

# Create a temporary file to store results
temp_file=$(mktemp)

# Function to convert total minutes to hours and minutes
format_time() {
  local total_minutes=$1
  printf "%dh %dm" $((total_minutes / 60)) $((total_minutes % 60))
}

# Function to process each show
process_show() {
  local show_name="$1"
  # Call C# app and capture runtime or error
  runtime=$("$GET_TVSHOW_TOTAL_LENGTH_BIN" "$show_name")
  status=$?

  echo "$runtime $show_name"
  if [ $status -eq 0 ]; then
    echo "$runtime $show_name" >> "$temp_file"
  else
    echo "Could not get info for $show_name." >&2
  fi
}

# Read TV shows from standard input
while IFS= read -r show_name || [ -n "$show_name" ]; do
  process_show "$show_name" &
done

# Wait for all background jobs to complete
wait

# Initialize variables for the shortest and longest shows
shortest_show=""
longest_show=""
shortest_time=0
longest_time=0

# Read results from the temporary file
while read -r line; do
  runtime=$(echo "$line" | awk '{print $1}')
  show_name=$(echo "$line" | cut -d' ' -f2-)

  # Check if this is the shortest or longest show
  if [ $shortest_time -eq 0 ] || [ $runtime -lt $shortest_time ]; then
    shortest_time=$runtime
    shortest_show=$show_name
  fi
  if [ $runtime -gt $longest_time ]; then
    longest_time=$runtime
    longest_show=$show_name
  fi
done < "$temp_file"

# Remove the temporary file
rm "$temp_file"

# Output results
if [ -n "$shortest_show" ] && [ -n "$longest_show" ]; then
  echo "The shortest show: $shortest_show ($(format_time $shortest_time))"
  echo "The longest show: $longest_show ($(format_time $longest_time))"
else
  echo "No shows processed successfully." >&2
fi
