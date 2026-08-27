"""Pure supervision comparisons; no CLI, Wine, or game is launched."""

import unittest
from smoke import same_observation


class SmokeObservationTests(unittest.TestCase):
    def test_transport_id_is_not_progress_but_script_id_is(self):
        first = {"pending": {"id": 1, "op": "run"}, "lastFullResponse": {"id": 1, "result": {"watches": {"id": 7}}}}
        second = {"pending": {"id": 2, "op": "run"}, "lastFullResponse": {"id": 2, "result": {"watches": {"id": 7}}}}
        self.assertTrue(same_observation(first, second))
        self.assertEqual(second["lastFullResponse"]["id"], 2, "raw snapshots must remain unchanged")
        second["lastFullResponse"]["result"]["watches"]["id"] = 8
        self.assertFalse(same_observation(first, second))

    def test_full_state_equality_detects_stall(self):
        state = {"phase": "request", "pending": {"op": "run"}, "lastFullResponse": {"result": {"output": ["a"]}}, "process": {"returncode": None}}
        self.assertFalse(same_observation(None, state))
        self.assertTrue(same_observation(state, dict(state)))
        changed = {**state, "lastFullResponse": {"result": {"output": ["b"]}}}
        self.assertFalse(same_observation(state, changed))


if __name__ == "__main__":
    unittest.main()
