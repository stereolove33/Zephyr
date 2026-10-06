import test from 'node:test';
import assert from 'node:assert/strict';
import {applyCustomOrder,moveCustomOrder} from './custom-order.mjs';
const mods=[{id:'skin',enabled:true},{id:'font',enabled:false},{id:'map',enabled:true}];
test('saved order puts the font first without changing activation or backend order',()=>{const ordered=applyCustomOrder(mods,['font','skin','map']);assert.deepEqual(ordered.map(m=>m.id),['font','skin','map']);assert.equal(ordered[0],mods[1]);assert.equal(ordered[0].enabled,false);assert.deepEqual(mods.map(m=>m.id),['skin','font','map']);});
test('new customs append and stale or duplicate saved IDs are harmless',()=>{assert.deepEqual(applyCustomOrder(mods,['removed','font','font']).map(m=>m.id),['font','skin','map']);});
test('malformed saved order uses the library order',()=>{assert.deepEqual(applyCustomOrder(mods,{bad:true}),mods);});
test('customs move upward and downward around the selected row',()=>{assert.deepEqual(moveCustomOrder(['skin','font','map'],'font','skin'),['font','skin','map']);assert.deepEqual(moveCustomOrder(['font','skin','map'],'font','map',true),['skin','map','font']);});
test('dropping onto itself or an unknown row leaves order intact',()=>{for(const [a,b]of [['font','font'],['bad','skin'],['skin','bad']])assert.deepEqual(moveCustomOrder(['skin','font','map'],a,b),['skin','font','map']);});
