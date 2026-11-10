import re
import sys
from zipfile import ZipFile
from xml.etree import ElementTree as ET

source, target = sys.argv[1:]
ns = '{http://schemas.openxmlformats.org/presentationml/2006/main}'
expected = {2: 'fade', 3: 'push', 4: 'fade', 5: 'fade', 6: 'push', 7: 'fade', 8: 'fade'}

with ZipFile(source) as original, ZipFile(target, 'w') as revised:
    for entry in original.infolist():
        payload = original.read(entry)
        match = re.fullmatch(r'ppt/slides/slide(\d+)\.xml', entry.filename)
        if match and (number := int(match.group(1))) in expected:
            slide = payload.decode('utf-8')
            effect = expected[number]
            child = '<p:push dir="l"/>' if effect == 'push' else '<p:fade/>'
            transition = f'<p:transition spd="med" advClick="1">{child}</p:transition>'
            slide, inserted = re.subn(r'(?=</p:sld>)', lambda _: transition, slide, count=1)
            assert inserted == 1, entry.filename
            payload = slide.encode('utf-8')
        revised.writestr(entry, payload)

with ZipFile(target) as result:
    assert result.testzip() is None
    for number in range(1, 9):
        slide = ET.fromstring(result.read(f'ppt/slides/slide{number}.xml'))
        transition = slide.find(ns + 'transition')
        if number in expected:
            assert transition is not None and transition[0].tag == ns + expected[number]
            assert 'advTm' not in transition.attrib
        else:
            assert transition is None
    print('Verified 8 slides and 7 click-to-advance transitions.')

