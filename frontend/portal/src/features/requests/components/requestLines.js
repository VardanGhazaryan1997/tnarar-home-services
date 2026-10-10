/** Request lines grouped by room, rooms in the order they first appear: `[{ name, lines }]`. */
export function groupByRoom(lines) {
  const rooms = []
  for (const line of lines) {
    const room = rooms.find((candidate) => candidate.name === line.roomName)
    if (room) room.lines.push(line)
    else rooms.push({ name: line.roomName, lines: [line] })
  }
  return rooms
}
