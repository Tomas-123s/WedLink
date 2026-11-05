import { describe, expect, it } from 'vitest'
import { invitationLocalDateToEpoch } from './invitationsApi'

describe('invitationLocalDateToEpoch', () => {
  it('converts a Cairo wall-clock event to its UTC instant', () => {
    expect(invitationLocalDateToEpoch('2026-11-01T12:00:00', 'Africa/Cairo'))
      .toBe(Date.UTC(2026, 10, 1, 10, 0, 0))
  })

  it('uses the time-zone offset before and after a daylight-saving transition', () => {
    expect(invitationLocalDateToEpoch('2026-03-29T01:30:00', 'Europe/Berlin'))
      .toBe(Date.UTC(2026, 2, 29, 0, 30, 0))
    expect(invitationLocalDateToEpoch('2026-03-29T03:30:00', 'Europe/Berlin'))
      .toBe(Date.UTC(2026, 2, 29, 1, 30, 0))
    expect(invitationLocalDateToEpoch('2026-10-25T03:30:00', 'Europe/Berlin'))
      .toBe(Date.UTC(2026, 9, 25, 2, 30, 0))
  })

  it('rejects a nonexistent local time in a daylight-saving gap', () => {
    expect(invitationLocalDateToEpoch('2026-03-29T02:30:00', 'Europe/Berlin')).toBeNull()
  })

  it('rejects malformed dates and unknown time zones', () => {
    expect(invitationLocalDateToEpoch('2026-02-30T12:00:00', 'Europe/Berlin')).toBeNull()
    expect(invitationLocalDateToEpoch('2026-10-25T03:30:00', 'Unknown/Zone')).toBeNull()
  })
})
