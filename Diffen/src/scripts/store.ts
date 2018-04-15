import Vue from 'vue'
import Vuex, { Module } from 'vuex'

import Forum from './modules/forum'
import Profile from './modules/profile'
import Squad from './modules/squad'

import { ViewModel } from './model/common'

Vue.use(Vuex)

interface State {
	vm: ViewModel
}
const state: State = {
	vm: new ViewModel()
}

const store = new Vuex.Store({
	state: state,
	modules: {
		forum: <Module<any, State>> Forum,
		profile: <Module<any, State>> Profile,
		squad: <Module<any, State>> Squad
	}
})

export default store