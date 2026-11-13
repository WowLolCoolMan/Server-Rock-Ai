import pathlib, tempfile, unittest, sys
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]))
import marketplace as m

class CatalogTests(unittest.TestCase):
    def test_catalog_250(self):
        a=m.catalog()
        self.assertEqual(len(a),250)
        self.assertEqual(len({(x['repo'],x['path']) for x in a}),250)
        self.assertEqual([x['number'] for x in a],list(range(1,251)))
    def test_expected_shub_skills(self):
        names={(e['repo'],e['path']) for e in m.catalog()}
        for skill in ('thinking-out-loud','dependency-doctor','scope-creep-detector'):
            self.assertIn(('Shubhamsaboo/awesome-llm-apps','agent_skills/'+skill),names)
    def test_path_security(self):
        x=m.catalog()[0].copy();x['path']='../../secrets'
        with self.assertRaises(ValueError):m.validate_entry(x)
    def test_snapshot_detects_edit(self):
        with tempfile.TemporaryDirectory() as d:
            p=pathlib.Path(d);(p/'SKILL.md').write_text('a')
            before=m.snapshot(p);(p/'SKILL.md').write_text('b')
            self.assertNotEqual(before,m.snapshot(p))
    def test_symlinks_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            p=pathlib.Path(d);(p/'SKILL.md').write_text('a')
            (p/'target').symlink_to(p/'SKILL.md')
            with self.assertRaises(ValueError):m.snapshot(p)
    def test_user_approval_required(self):
        with self.assertRaises(ValueError):m.install(m.catalog()[0],approval=False)

if __name__=='__main__':unittest.main()
